import { TestBed, fakeAsync, flushMicrotasks, tick } from '@angular/core/testing';
import { HttpTestingController } from '@angular/common/http/testing';
import { VxTestingModule } from 'src/testing/vx-testing.module';
import { environment } from 'src/environments/environment';
import { CONNECTION_FALLBACK_POLL_MS, ChargerConnectionService } from './charger-connection.service';
import { HUB_CONNECTION_FACTORY, HUB_IDLE_CLOSE_MS } from './realtime-hub.service';
import { FakeHubConnection } from 'src/testing/fake-hub-connection';

describe('ChargerConnectionService', () => {
  let service: ChargerConnectionService;
  let http: HttpTestingController;
  let connection: FakeHubConnection;
  const url = (id: string) => `${environment.apiUrl}/api/chargePointRealTime/status/${id}`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [{ provide: HUB_CONNECTION_FACTORY, useValue: (u: string, t: () => Promise<string>) => connection = new FakeHubConnection(u, t) }]
    });
    service = TestBed.inject(ChargerConnectionService);
    http = TestBed.inject(HttpTestingController);
  });

  it('reads the status at once, follows the pushes of that charger only, and stops with its last subscriber', fakeAsync(() => {
    const seen: (boolean | null)[] = [];
    const sub = service.status('CP-1').subscribe(s => seen.push(s));
    tick();
    http.expectOne(url('CP-1')).flush({ chargePointID: 'CP-1', isActive: false });
    flushMicrotasks();
    expect(connection.joined()).toEqual([['CP-1']]);

    connection.emit('ChargerConnection', { chargePointId: 'CP-2', isActive: true });
    connection.emit('ChargerConnection', { chargePointId: 'CP-1', isActive: true });
    expect(seen).toEqual([false, true]);

    tick(CONNECTION_FALLBACK_POLL_MS);
    http.expectOne(url('CP-1')).flush({ isActive: true });
    expect(seen).toEqual([false, true]);

    sub.unsubscribe();
    expect(connection.left()).toEqual([['CP-1']]);
    tick(CONNECTION_FALLBACK_POLL_MS + HUB_IDLE_CLOSE_MS);
    http.expectNone(url('CP-1'));
  }));

  it('shares one stream between pages watching the same charger and reports an unreadable status as unknown', fakeAsync(() => {
    const a: (boolean | null)[] = [];
    const b: (boolean | null)[] = [];
    const subA = service.status('CP-9').subscribe(s => a.push(s));
    const subB = service.status('CP-9').subscribe(s => b.push(s));
    tick();
    http.expectOne(url('CP-9')).flush('boom', { status: 500, statusText: 'Server Error' });
    expect(a).toEqual([null]);
    expect(b).toEqual([null]);
    subA.unsubscribe();
    subB.unsubscribe();
    tick(HUB_IDLE_CLOSE_MS);
  }));
});
