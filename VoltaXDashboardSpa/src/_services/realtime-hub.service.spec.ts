import { TestBed, fakeAsync, flushMicrotasks, tick } from '@angular/core/testing';
import { of } from 'rxjs';
import { VxTestingModule } from 'src/testing/vx-testing.module';
import { FakeHubConnection, fakeHubFactory } from 'src/testing/fake-hub-connection';
import { HUB_CONNECTION_FACTORY, HUB_IDLE_CLOSE_MS, HubState, RealtimeHubService, chargerGroup } from './realtime-hub.service';
import { TokenRefreshService } from './token-refresh.service';
import { TokenStorageService } from './token-storage.service';

describe('RealtimeHubService', () => {
  let service: RealtimeHubService;
  let connections: FakeHubConnection[];

  beforeEach(() => {
    connections = [];
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [{ provide: HUB_CONNECTION_FACTORY, useValue: fakeHubFactory(connections) }]
    });
    service = TestBed.inject(RealtimeHubService);
  });

  it('opens one connection per hub for all listeners and delivers the events', fakeAsync(() => {
    const a: unknown[] = [];
    const b: unknown[] = [];
    const subA = service.on('charger', 'AlarmChanged').subscribe(e => a.push(e));
    const subB = service.on('charger', 'ChargerEvent').subscribe(e => b.push(e));
    flushMicrotasks();

    expect(connections.length).toBe(1);
    expect(connections[0].url).toContain('/chargerHub');
    expect(connections[0].starts).toBe(1);
    expect(service.state('charger')).toBe('connected');

    connections[0].emit('AlarmChanged', { id: 1 });
    connections[0].emit('ChargerEvent', { id: 2 });
    expect(a).toEqual([{ id: 1 }]);
    expect(b).toEqual([{ id: 2 }]);
    subA.unsubscribe();
    subB.unsubscribe();
    tick(HUB_IDLE_CLOSE_MS);
  }));

  it('joins a group once while listened to, again after a reconnection, and leaves it with the last listener', fakeAsync(() => {
    const first = service.on('charger', 'SentMessage', chargerGroup('CP-1')).subscribe();
    const second = service.on('charger', 'ReceiveMessage', chargerGroup('CP-1')).subscribe();
    flushMicrotasks();
    const connection = connections[0];
    expect(connection.joined()).toEqual([['CP-1']]);

    let reloads = 0;
    const reconnected = service.reconnected('charger').subscribe(() => reloads++);
    connection.dropAndRecover();
    expect(connection.joined()).toEqual([['CP-1'], ['CP-1']]);
    expect(reloads).toBe(1);

    first.unsubscribe();
    expect(connection.left()).toEqual([]);
    second.unsubscribe();
    expect(connection.left()).toEqual([['CP-1']]);
    reconnected.unsubscribe();

    expect(connection.stops).toBe(0);
    tick(HUB_IDLE_CLOSE_MS);
    expect(connection.stops).toBe(1);
    expect(service.state('charger')).toBe('idle');
  }));

  it('keeps the connection when a page is left and another one opened right away', fakeAsync(() => {
    service.on('charger', 'AlarmChanged').subscribe().unsubscribe();
    flushMicrotasks();
    tick(HUB_IDLE_CLOSE_MS / 2);
    const again = service.on('charger', 'AlarmChanged').subscribe();
    tick(HUB_IDLE_CLOSE_MS);
    expect(connections.length).toBe(1);
    expect(connections[0].stops).toBe(0);
    again.unsubscribe();
    tick(HUB_IDLE_CLOSE_MS);
  }));

  it('retries a connection that cannot start and reports it meanwhile', fakeAsync(() => {
    const states: HubState[] = [];
    const status = service.status$.subscribe(s => states.push(s));
    const sub = service.on('notification', 'NotificationReceived').subscribe();
    connections[0].failStarts = 0;
    flushMicrotasks();
    expect(states[states.length - 1]).toBe('connected');

    connections[0].failStarts = 1;
    connections[0].giveUp();
    expect(service.state('notification')).toBe('disconnected');
    expect(states).toContain('disconnected');
    tick(2000);
    flushMicrotasks();
    expect(service.state('notification')).toBe('disconnected');
    tick(5000);
    flushMicrotasks();
    expect(connections[0].starts).toBe(3);
    expect(service.state('notification')).toBe('connected');

    connections[0].drop();
    expect(service.state('notification')).toBe('reconnecting');
    sub.unsubscribe();
    status.unsubscribe();
    tick(HUB_IDLE_CLOSE_MS);
  }));

  it('closes every connection on sign out', fakeAsync(() => {
    service.on('charger', 'AlarmChanged').subscribe();
    service.on('notification', 'NotificationReceived').subscribe();
    flushMicrotasks();
    service.stopAll();
    flushMicrotasks();
    expect(connections.map(c => c.stops)).toEqual([1, 1]);
    expect(service.state('charger')).toBe('idle');
  }));

  it('refreshes an access token about to expire before connecting', fakeAsync(() => {
    const tokens = TestBed.inject(TokenStorageService);
    spyOn(tokens, 'isAccessTokenAboutToExpire').and.returnValue(true);
    spyOn(tokens, 'hasRefreshToken').and.returnValue(true);
    const refresh = spyOn(TestBed.inject(TokenRefreshService), 'refresh').and.callFake(() => {
      tokens.storeAccessToken('fresh-token');
      return of('fresh-token');
    });
    service.on('charger', 'AlarmChanged').subscribe();
    let token = '';
    connections[0].accessToken().then(t => token = t);
    flushMicrotasks();
    expect(refresh).toHaveBeenCalled();
    expect(token).toBe('fresh-token');
    service.stopAll();
    flushMicrotasks();
  }));
});
