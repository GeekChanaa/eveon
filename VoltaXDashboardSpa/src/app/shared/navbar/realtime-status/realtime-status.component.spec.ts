import { ComponentFixture, TestBed, fakeAsync, flushMicrotasks, tick } from '@angular/core/testing';
import { Subscription } from 'rxjs';
import { HUB_CONNECTION_FACTORY, HUB_IDLE_CLOSE_MS, RealtimeHubService } from 'src/_services/realtime-hub.service';
import { FakeHubConnection, fakeHubFactory } from 'src/testing/fake-hub-connection';
import { VxTestingModule } from 'src/testing/vx-testing.module';
import { RealtimeStatusComponent } from './realtime-status.component';

describe('RealtimeStatusComponent', () => {
  let fixture: ComponentFixture<RealtimeStatusComponent>;
  let connections: FakeHubConnection[];
  let hub: RealtimeHubService;
  let listening: Subscription;

  beforeEach(() => {
    connections = [];
    TestBed.configureTestingModule({
      imports: [VxTestingModule, RealtimeStatusComponent],
      providers: [{ provide: HUB_CONNECTION_FACTORY, useValue: fakeHubFactory(connections) }]
    });
    hub = TestBed.inject(RealtimeHubService);
    fixture = TestBed.createComponent(RealtimeStatusComponent);
  });

  const badge = () => fixture.nativeElement.querySelector('.realtime-status') as HTMLButtonElement | null;

  it('stays hidden while connected and through a short reconnection', fakeAsync(() => {
    listening = hub.on('charger', 'AlarmChanged').subscribe();
    flushMicrotasks();
    fixture.detectChanges();
    expect(badge()).toBeNull();

    connections[0].drop();
    tick(500);
    connections[0].dropAndRecover();
    tick(2000);
    fixture.detectChanges();
    expect(badge()).toBeNull();
    listening.unsubscribe();
    tick(HUB_IDLE_CLOSE_MS + 2000);
  }));

  it('shows a lasting reconnection, then a paused state that retries on click', fakeAsync(() => {
    listening = hub.on('charger', 'AlarmChanged').subscribe();
    flushMicrotasks();
    fixture.detectChanges();

    connections[0].drop();
    tick(1600);
    fixture.detectChanges();
    expect(badge()?.textContent).toContain('operator.realtime.reconnecting');

    connections[0].failStarts = 5;
    connections[0].giveUp();
    tick(1600);
    fixture.detectChanges();
    expect(badge()?.classList).toContain('realtime-status--down');
    expect(badge()?.textContent).toContain('operator.realtime.disconnected');

    connections[0].failStarts = 0;
    badge()!.click();
    flushMicrotasks();
    fixture.detectChanges();
    expect(badge()).toBeNull();
    listening.unsubscribe();
    tick(HUB_IDLE_CLOSE_MS + 2000);
  }));
});
