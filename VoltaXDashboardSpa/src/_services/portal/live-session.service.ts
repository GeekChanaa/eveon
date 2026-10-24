import { HttpClient } from '@angular/common/http';
import { Inject, Injectable, InjectionToken, NgZone } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Observable, Subject, filter, firstValueFrom } from 'rxjs';
import { environment } from 'src/environments/environment';
import { CommandAnswer } from 'src/app/shared/portal-ui/remote-command';
import { TokenRefreshService } from '../token-refresh.service';
import { TokenStorageService } from '../token-storage.service';

/** A pushed update of a charging session (ChargingSessionUpdateDto on /chargingSessionHub). */
export interface LiveSessionUpdate {
  sessionId: number;
  transactionUid: string | null;
  /** Started, Charging, StoppingLowBalance or Ended. */
  status: string;
  energyKWh: number;
  /** Power drawn since the previous meter reading; null until two readings are known. */
  powerKw: number | null;
  durationMinutes: number;
  currentCost: number;
  cardBalance: number;
  stopReason: string | null;
  timestamp: string;
}

export type LiveConnectionState = 'idle' | 'connecting' | 'live' | 'reconnecting' | 'offline';

/** The part of a SignalR connection the service uses (replaced by a fake in unit tests). */
export interface LiveHubConnection {
  on(event: string, handler: (...args: any[]) => void): void;
  onreconnecting(callback: () => void): void;
  onreconnected(callback: () => void): void;
  onclose(callback: () => void): void;
  start(): Promise<void>;
  stop(): Promise<void>;
}

export type LiveHubFactory = (accessToken: () => Promise<string>) => LiveHubConnection;

/** Builds the connection to the charging session hub; the server adds it to the signed-in user's group. */
export const LIVE_HUB_FACTORY = new InjectionToken<LiveHubFactory>('LIVE_HUB_FACTORY', {
  providedIn: 'root',
  factory: () => (accessToken: () => Promise<string>) => new signalR.HubConnectionBuilder()
    .withUrl(environment.apiUrl + '/chargingSessionHub', { accessTokenFactory: accessToken })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .build()
});

const RETRY_MS = 15000;

/**
 * The customer portal's live view of the signed-in user's charging sessions: energy, power, cost and status pushed
 * by the API as the charger reports them, and the remote stop of one's own session. Only this portal uses it, so it
 * keeps its own small connection (opened while a live widget is on screen, reference counted).
 */
@Injectable({ providedIn: 'root' })
export class PortalLiveSessionService {
  private hub?: LiveHubConnection;
  private followers = 0;
  private retryTimer?: ReturnType<typeof setTimeout>;
  private readonly updatesSubject = new Subject<LiveSessionUpdate>();
  private readonly stateSubject = new BehaviorSubject<LiveConnectionState>('idle');

  readonly updates$: Observable<LiveSessionUpdate> = this.updatesSubject.asObservable();
  readonly state$: Observable<LiveConnectionState> = this.stateSubject.asObservable();

  constructor(
    private http: HttpClient,
    private tokens: TokenStorageService,
    private tokenRefresh: TokenRefreshService,
    private zone: NgZone,
    @Inject(LIVE_HUB_FACTORY) private factory: LiveHubFactory
  ) {}

  get state(): LiveConnectionState { return this.stateSubject.value; }

  /** Starts receiving the user's session updates; each follow() is matched by a release(). */
  follow(): void {
    if (this.followers++ > 0) return;
    this.open();
  }

  release(): void {
    if (this.followers === 0 || --this.followers > 0) return;
    clearTimeout(this.retryTimer);
    const hub = this.hub;
    this.hub = undefined;
    hub?.stop().catch(() => undefined);
    this.stateSubject.next('idle');
  }

  /** The updates of one session. */
  session$(sessionId: number): Observable<LiveSessionUpdate> {
    return this.updates$.pipe(filter(update => update.sessionId === sessionId));
  }

  /** Asks the charger to stop the caller's own session; the API refuses (404) a session that is not theirs. */
  stop(chargePointId: string, transactionUid: string): Observable<CommandAnswer> {
    return this.http.post<CommandAnswer>(`${environment.apiUrl}/ocpp/EVDriver/RequestStopTransactionMobile/${encodeURIComponent(chargePointId)}`,
      { transactionId: transactionUid });
  }

  private open(): void {
    const hub = this.factory(() => this.accessToken());
    this.hub = hub;
    hub.on('SessionUpdate', (update: LiveSessionUpdate) => this.zone.run(() => this.updatesSubject.next(update)));
    hub.onreconnecting(() => this.zone.run(() => this.stateSubject.next('reconnecting')));
    hub.onreconnected(() => this.zone.run(() => this.stateSubject.next('live')));
    hub.onclose(() => this.zone.run(() => { if (this.hub === hub) this.stateSubject.next('offline'); }));
    this.start(hub);
  }

  private start(hub: LiveHubConnection): void {
    this.stateSubject.next('connecting');
    hub.start().then(
      () => { if (this.hub === hub) this.stateSubject.next('live'); },
      () => {
        if (this.hub !== hub) return;
        // withAutomaticReconnect only covers drops after a successful start.
        this.stateSubject.next('offline');
        this.retryTimer = setTimeout(() => this.start(hub), RETRY_MS);
      });
  }

  private async accessToken(): Promise<string> {
    if (this.tokens.isAccessTokenAboutToExpire() && this.tokens.hasRefreshToken()) {
      try { await firstValueFrom(this.tokenRefresh.refresh()); } catch { /* the API answers 401, the hub retries */ }
    }
    return this.tokens.accessToken || '';
  }
}
