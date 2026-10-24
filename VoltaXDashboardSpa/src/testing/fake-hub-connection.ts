import { HubConnectionLike, HubConnectionFactory } from 'src/_services/realtime-hub.service';

/** A SignalR connection driven by the test (provide it through HUB_CONNECTION_FACTORY). */
export class FakeHubConnection implements HubConnectionLike {
  readonly handlers = new Map<string, Set<(...args: any[]) => void>>();
  readonly invoked: { method: string; args: unknown[] }[] = [];
  starts = 0;
  stops = 0;
  /** Number of next start() calls that fail. */
  failStarts = 0;
  private reconnectingCallback?: () => void;
  private reconnectedCallback?: () => void;
  private closeCallback?: () => void;

  constructor(readonly url: string, readonly accessToken: () => Promise<string>) {}

  start(): Promise<void> {
    this.starts++;
    return this.failStarts-- > 0 ? Promise.reject(new Error('down')) : Promise.resolve();
  }
  stop(): Promise<void> { this.stops++; return Promise.resolve(); }
  invoke(method: string, ...args: unknown[]): Promise<unknown> { this.invoked.push({ method, args }); return Promise.resolve(); }
  on(event: string, handler: (...args: any[]) => void): void {
    if (!this.handlers.has(event)) this.handlers.set(event, new Set());
    this.handlers.get(event)!.add(handler);
  }
  off(event: string, handler: (...args: any[]) => void): void { this.handlers.get(event)?.delete(handler); }
  onreconnecting(callback: () => void): void { this.reconnectingCallback = callback; }
  onreconnected(callback: () => void): void { this.reconnectedCallback = callback; }
  onclose(callback: () => void): void { this.closeCallback = callback; }

  /** The server pushes an event. */
  emit(event: string, payload: unknown): void { this.handlers.get(event)?.forEach(handler => handler(payload)); }
  /** The connection drops and automatic reconnection restores it. */
  dropAndRecover(): void { this.reconnectingCallback?.(); this.reconnectedCallback?.(); }
  /** The connection drops (automatic reconnection running). */
  drop(): void { this.reconnectingCallback?.(); }
  /** Automatic reconnection gave up. */
  giveUp(): void { this.closeCallback?.(); }
  joined(): unknown[][] { return this.invoked.filter(i => i.method === 'JoinChargerGroup').map(i => i.args); }
  left(): unknown[][] { return this.invoked.filter(i => i.method === 'LeaveChargerGroup').map(i => i.args); }
}

/** A HUB_CONNECTION_FACTORY that records the fake connections it creates. */
export function fakeHubFactory(created: FakeHubConnection[] = []): HubConnectionFactory {
  return (url, accessToken) => {
    const connection = new FakeHubConnection(url, accessToken);
    created.push(connection);
    return connection;
  };
}
