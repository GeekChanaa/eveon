import { Inject, Injectable, InjectionToken, NgZone, OnDestroy } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Observable, Subject, combineLatest, distinctUntilChanged, filter, firstValueFrom, map, of, switchMap } from 'rxjs';
import { environment } from 'src/environments/environment';
import { TokenRefreshService } from './token-refresh.service';
import { TokenStorageService } from './token-storage.service';

/** The API's SignalR hubs. */
export type HubName = 'charger' | 'chargingSession' | 'notification';

const HUB_PATHS: Record<HubName, string> = {
  charger: '/chargerHub',
  chargingSession: '/chargingSessionHub',
  notification: '/notificationHub'
};

/** idle: nobody listens; reconnecting: the connection dropped and is being restored; disconnected: gave up for now (retried). */
export type HubState = 'idle' | 'connecting' | 'connected' | 'reconnecting' | 'disconnected';

/** A hub group the client belongs to while it listens: joined after every (re)connection, left when the last listener leaves. */
export interface HubGroup {
  join: string;
  leave: string;
  args: unknown[];
}

/** The charger hub group of one charge point (live OCPP messages, connection status), or a feed group such as "#alarms". */
export function chargerGroup(chargePointId: string): HubGroup {
  return { join: 'JoinChargerGroup', leave: 'LeaveChargerGroup', args: [chargePointId] };
}

/** What the service needs from a SignalR connection (a HubConnection; a fake in unit tests). */
export interface HubConnectionLike {
  start(): Promise<void>;
  stop(): Promise<void>;
  invoke(method: string, ...args: any[]): Promise<any>;
  on(event: string, handler: (...args: any[]) => void): void;
  off(event: string, handler: (...args: any[]) => void): void;
  onreconnecting(callback: (error?: Error) => void): void;
  onreconnected(callback: (connectionId?: string) => void): void;
  onclose(callback: (error?: Error) => void): void;
}

export type HubConnectionFactory = (url: string, accessToken: () => Promise<string>) => HubConnectionLike;

/** Builds the real connections: automatic reconnection, access token from the token services. */
export const HUB_CONNECTION_FACTORY = new InjectionToken<HubConnectionFactory>('SignalR connection factory', {
  providedIn: 'root',
  factory: () => (url, accessToken) => new signalR.HubConnectionBuilder()
    .withUrl(url, { accessTokenFactory: accessToken })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(environment.production ? signalR.LogLevel.None : signalR.LogLevel.Warning)
    .build()
});

/** Time a connection stays open after its last listener left (navigating between pages keeps it). */
export const HUB_IDLE_CLOSE_MS = 3000;
/** Delays before restarting a connection that could not start or that automatic reconnection gave up on. */
const RESTART_DELAYS_MS = [2000, 5000, 15000, 30000, 60000];

class HubEntry {
  listeners = 0;
  readonly groups = new Map<string, { group: HubGroup; count: number }>();
  readonly state = new BehaviorSubject<HubState>('idle');
  readonly reconnected = new Subject<void>();
  stopping = false;
  attempt = 0;
  restartTimer?: ReturnType<typeof setTimeout>;
  closeTimer?: ReturnType<typeof setTimeout>;
  constructor(readonly connection: HubConnectionLike) {}
}

/**
 * The single owner of the dashboard's SignalR connections: one connection per hub, opened for its first listener and
 * closed shortly after the last one leaves, reconnected automatically (groups joined again), authenticated with the
 * in-memory access token (refreshed when about to expire) and closed on sign out. {@link status$} feeds the
 * "live updates" indicator.
 */
@Injectable({ providedIn: 'root' })
export class RealtimeHubService implements OnDestroy {
  private readonly hubs = new Map<HubName, HubEntry>();
  private readonly hubsChanged = new BehaviorSubject<void>(undefined);

  /** Worst state of the hubs in use: disconnected, then reconnecting, connecting, connected; idle when none is used. */
  readonly status$: Observable<HubState> = this.hubsChanged.pipe(
    switchMap(() => {
      const states = [...this.hubs.values()].map(entry => entry.state);
      return states.length ? combineLatest(states) : of<HubState[]>([]);
    }),
    map(states => worst(states)),
    distinctUntilChanged()
  );

  constructor(
    private tokens: TokenStorageService,
    private tokenRefresh: TokenRefreshService,
    private zone: NgZone,
    @Inject(HUB_CONNECTION_FACTORY) private createConnection: HubConnectionFactory
  ) {}

  /**
   * Server events of a hub. Subscribing opens the connection (once for all listeners) and joins the groups;
   * unsubscribing leaves them. Events are delivered inside the Angular zone.
   */
  on<T>(hub: HubName, event: string, ...groups: HubGroup[]): Observable<T> {
    return new Observable<T>(subscriber => {
      const entry = this.acquire(hub);
      const handler = (payload: T) => this.zone.run(() => subscriber.next(payload));
      entry.connection.on(event, handler);
      groups.forEach(group => this.addGroup(entry, group));
      return () => {
        entry.connection.off(event, handler);
        groups.forEach(group => this.removeGroup(entry, group));
        this.release(hub, entry);
      };
    });
  }

  /** Fires after the hub reconnected: reload what may have been missed meanwhile. */
  reconnected(hub: HubName): Observable<void> {
    return new Observable<void>(subscriber => {
      const entry = this.acquire(hub);
      const subscription = entry.reconnected.subscribe(() => subscriber.next());
      return () => {
        subscription.unsubscribe();
        this.release(hub, entry);
      };
    });
  }

  /** State of one hub ('idle' while nobody listens). */
  state(hub: HubName): HubState {
    return this.hubs.get(hub)?.state.value ?? 'idle';
  }

  /** Calls a hub method once the connection is up (the hub must have a listener). */
  async invoke<T = unknown>(hub: HubName, method: string, ...args: unknown[]): Promise<T> {
    const entry = this.hubs.get(hub);
    if (!entry) throw new Error(`No listener on the ${hub} hub`);
    await firstValueFrom(entry.state.pipe(filter(state => state === 'connected')));
    return entry.connection.invoke(method, ...args);
  }

  /** Restarts the hubs that gave up, now (the indicator's retry button). */
  retryNow(): void {
    this.hubs.forEach(entry => {
      if (entry.state.value === 'disconnected' && entry.listeners > 0) {
        clearTimeout(entry.restartTimer);
        entry.attempt = 0;
        this.start(entry);
      }
    });
  }

  /** Closes every connection (sign out). Listeners still subscribed get no more events. */
  async stopAll(): Promise<void> {
    const entries = [...this.hubs.values()];
    this.hubs.clear();
    this.hubsChanged.next();
    await Promise.all(entries.map(entry => this.close(entry)));
  }

  ngOnDestroy(): void {
    void this.stopAll();
  }

  private acquire(hub: HubName): HubEntry {
    let entry = this.hubs.get(hub);
    if (!entry) {
      entry = this.create(hub);
      this.hubs.set(hub, entry);
      this.hubsChanged.next();
    }
    entry.listeners++;
    clearTimeout(entry.closeTimer);
    entry.closeTimer = undefined;
    if (entry.state.value === 'idle') this.start(entry);
    return entry;
  }

  private release(hub: HubName, entry: HubEntry): void {
    entry.listeners = Math.max(0, entry.listeners - 1);
    if (entry.listeners > 0 || this.hubs.get(hub) !== entry) return;
    clearTimeout(entry.closeTimer);
    entry.closeTimer = setTimeout(() => {
      if (entry.listeners > 0 || this.hubs.get(hub) !== entry) return;
      this.hubs.delete(hub);
      this.hubsChanged.next();
      void this.close(entry);
    }, HUB_IDLE_CLOSE_MS);
  }

  private create(hub: HubName): HubEntry {
    const connection = this.createConnection(environment.apiUrl + HUB_PATHS[hub], () => this.accessToken());
    const entry = new HubEntry(connection);
    connection.onreconnecting(() => this.zone.run(() => entry.state.next('reconnecting')));
    connection.onreconnected(() => this.zone.run(() => {
      entry.state.next('connected');
      this.joinAll(entry);
      entry.reconnected.next();
    }));
    connection.onclose(() => this.zone.run(() => {
      if (entry.stopping) return;
      // Automatic reconnection gave up (or the server closed the connection): try again later while listened to.
      entry.state.next('disconnected');
      this.scheduleRestart(entry);
    }));
    return entry;
  }

  private start(entry: HubEntry): void {
    if (entry.stopping) return;
    entry.state.next('connecting');
    entry.connection.start().then(
      () => this.zone.run(() => {
        if (entry.stopping) return;
        const wasRetry = entry.attempt > 0;
        entry.attempt = 0;
        entry.state.next('connected');
        this.joinAll(entry);
        if (wasRetry) entry.reconnected.next();
      }),
      () => this.zone.run(() => {
        if (entry.stopping) return;
        entry.state.next('disconnected');
        this.scheduleRestart(entry);
      }));
  }

  private scheduleRestart(entry: HubEntry): void {
    clearTimeout(entry.restartTimer);
    if (entry.stopping || entry.listeners === 0) return;
    const delay = RESTART_DELAYS_MS[Math.min(entry.attempt, RESTART_DELAYS_MS.length - 1)];
    entry.attempt++;
    entry.restartTimer = setTimeout(() => this.start(entry), delay);
  }

  private async close(entry: HubEntry): Promise<void> {
    entry.stopping = true;
    clearTimeout(entry.restartTimer);
    clearTimeout(entry.closeTimer);
    entry.state.next('idle');
    try { await entry.connection.stop(); } catch { /* already closed */ }
  }

  private addGroup(entry: HubEntry, group: HubGroup): void {
    const key = groupKey(group);
    const current = entry.groups.get(key);
    if (current) { current.count++; return; }
    entry.groups.set(key, { group, count: 1 });
    if (entry.state.value === 'connected') this.join(entry, group);
  }

  private removeGroup(entry: HubEntry, group: HubGroup): void {
    const key = groupKey(group);
    const current = entry.groups.get(key);
    if (!current || --current.count > 0) return;
    entry.groups.delete(key);
    if (entry.state.value === 'connected') entry.connection.invoke(group.leave, ...group.args).catch(() => undefined);
  }

  private joinAll(entry: HubEntry): void {
    entry.groups.forEach(({ group }) => this.join(entry, group));
  }

  private join(entry: HubEntry, group: HubGroup): void {
    // A refused join (no permission) only means no events for that group.
    entry.connection.invoke(group.join, ...group.args).catch(() => undefined);
  }

  private async accessToken(): Promise<string> {
    if (this.tokens.isAccessTokenAboutToExpire() && this.tokens.hasRefreshToken()) {
      try { await firstValueFrom(this.tokenRefresh.refresh()); } catch { /* the hub answers 401 and reconnects later */ }
    }
    return this.tokens.accessToken || '';
  }
}

function groupKey(group: HubGroup): string {
  return group.join + ':' + JSON.stringify(group.args);
}

const SEVERITY: Record<HubState, number> = { idle: 0, connected: 1, connecting: 2, reconnecting: 3, disconnected: 4 };

function worst(states: HubState[]): HubState {
  return states.reduce<HubState>((acc, state) => SEVERITY[state] > SEVERITY[acc] ? state : acc, 'idle');
}
