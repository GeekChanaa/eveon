import { TestBed, fakeAsync, flushMicrotasks, tick } from '@angular/core/testing';
import { HttpTestingController } from '@angular/common/http/testing';
import { environment } from 'src/environments/environment';
import { VxTestingModule } from 'src/testing/vx-testing.module';
import { LIVE_HUB_FACTORY, LiveHubConnection, LiveSessionUpdate, PortalLiveSessionService } from './live-session.service';

/** A SignalR connection the test drives by hand. */
export class FakeHub implements LiveHubConnection {
  handlers: Record<string, (...args: any[]) => void> = {};
  closeHandler?: () => void;
  reconnecting?: () => void;
  reconnected?: () => void;
  started = 0;
  stopped = 0;
  startResult: Promise<void> = Promise.resolve();

  on(event: string, handler: (...args: any[]) => void): void { this.handlers[event] = handler; }
  onreconnecting(callback: () => void): void { this.reconnecting = callback; }
  onreconnected(callback: () => void): void { this.reconnected = callback; }
  onclose(callback: () => void): void { this.closeHandler = callback; }
  start(): Promise<void> { this.started++; return this.startResult; }
  stop(): Promise<void> { this.stopped++; return Promise.resolve(); }

  push(update: Partial<LiveSessionUpdate>): void {
    this.handlers['SessionUpdate']({ sessionId: 1, transactionUid: 'TX', status: 'Charging', energyKWh: 0, powerKw: null, durationMinutes: 0,
      currentCost: 0, cardBalance: 0, stopReason: null, timestamp: '2026-10-23T10:00:00Z', ...update });
  }
}

describe('PortalLiveSessionService', () => {
  let hubs: FakeHub[];
  let service: PortalLiveSessionService;

  beforeEach(() => {
    hubs = [];
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [{ provide: LIVE_HUB_FACTORY, useValue: () => { const hub = new FakeHub(); hubs.push(hub); return hub; } }]
    });
    service = TestBed.inject(PortalLiveSessionService);
  });

  it('opens one connection for all the widgets on screen and closes it with the last one', fakeAsync(() => {
    service.follow();
    service.follow();
    flushMicrotasks();

    expect(hubs.length).toBe(1);
    expect(service.state).toBe('live');

    service.release();
    expect(hubs[0].stopped).toBe(0);
    service.release();
    expect(hubs[0].stopped).toBe(1);
    expect(service.state).toBe('idle');
  }));

  it('passes on the session updates the server pushes, per session', fakeAsync(() => {
    const all: LiveSessionUpdate[] = [];
    const mine: LiveSessionUpdate[] = [];
    service.updates$.subscribe(u => all.push(u));
    service.session$(7).subscribe(u => mine.push(u));
    service.follow();
    flushMicrotasks();

    hubs[0].push({ sessionId: 7, energyKWh: 2.5, powerKw: 11 });
    hubs[0].push({ sessionId: 8 });

    expect(all.length).toBe(2);
    expect(mine.map(u => u.energyKWh)).toEqual([2.5]);
    service.release();
  }));

  it('tells when the live updates are interrupted and back', fakeAsync(() => {
    service.follow();
    flushMicrotasks();

    hubs[0].reconnecting!();
    expect(service.state).toBe('reconnecting');
    hubs[0].reconnected!();
    expect(service.state).toBe('live');
    hubs[0].closeHandler!();
    expect(service.state).toBe('offline');
    service.release();
  }));

  it('retries later when the first connection fails', fakeAsync(() => {
    const failing = new FakeHub();
    failing.startResult = Promise.reject(new Error('down'));
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({ imports: [VxTestingModule], providers: [{ provide: LIVE_HUB_FACTORY, useValue: () => failing }] });
    const live = TestBed.inject(PortalLiveSessionService);

    live.follow();
    flushMicrotasks();
    expect(live.state).toBe('offline');

    failing.startResult = Promise.resolve();
    tick(15000);
    flushMicrotasks();
    expect(failing.started).toBe(2);
    expect(live.state).toBe('live');
    live.release();
    expect(failing.stopped).toBe(1);
  }));

  it('stops the caller\'s own session through the driver endpoint', () => {
    const http = TestBed.inject(HttpTestingController);
    let status: string | null = null;

    service.stop('CP 1', 'TX-9').subscribe(answer => status = answer.status);

    const request = http.expectOne(`${environment.apiUrl}/ocpp/EVDriver/RequestStopTransactionMobile/CP%201`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ transactionId: 'TX-9' });
    request.flush({ message: 'ok', status: 'Accepted' });
    expect(status).toBe('Accepted' as any);
    http.verify();
  });
});
