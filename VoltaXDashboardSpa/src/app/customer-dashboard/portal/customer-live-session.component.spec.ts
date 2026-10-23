import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BehaviorSubject, Subject, of, throwError } from 'rxjs';
import { configureVxTest } from 'src/testing/vx-testing.module';
import { MyActiveSession } from 'src/_models/portal';
import { LiveConnectionState, LiveSessionUpdate, PortalLiveSessionService } from 'src/_services/portal/live-session.service';
import { PortalUiModule } from 'src/app/shared/portal-ui/portal-ui.module';
import { CustomerLiveSessionComponent } from './customer-live-session.component';

const session = (extra: Partial<MyActiveSession> = {}): MyActiveSession => ({
  id: 5, stationName: 'Marina', chargePointName: 'CP-1', connector: 'EVSE 1', startDate: new Date(Date.now() - 10 * 60_000).toISOString(),
  chargedMinutes: 10, chargedKwhs: 2, currentCost: 5, transactionUid: 'TX-5', cardBalance: 100, ...extra
});

const update = (extra: Partial<LiveSessionUpdate> = {}): LiveSessionUpdate => ({
  sessionId: 5, transactionUid: 'TX-5', status: 'Charging', energyKWh: 3, powerKw: 22, durationMinutes: 12, currentCost: 7.5, cardBalance: 92.5,
  stopReason: null, timestamp: new Date().toISOString(), ...extra
});

describe('CustomerLiveSessionComponent', () => {
  let fixture: ComponentFixture<CustomerLiveSessionComponent>;
  let widget: CustomerLiveSessionComponent;
  let live: jasmine.SpyObj<PortalLiveSessionService>;
  let updates: Subject<LiveSessionUpdate>;
  let state: BehaviorSubject<LiveConnectionState>;

  beforeEach(async () => {
    updates = new Subject();
    state = new BehaviorSubject<LiveConnectionState>('live');
    live = jasmine.createSpyObj<PortalLiveSessionService>('PortalLiveSessionService', ['follow', 'release', 'stop'], { updates$: updates, state$: state });
    await configureVxTest([CustomerLiveSessionComponent], { imports: [PortalUiModule], providers: [{ provide: PortalLiveSessionService, useValue: live }] });
    fixture = TestBed.createComponent(CustomerLiveSessionComponent);
    widget = fixture.componentInstance;
    widget.session = session();
    fixture.detectChanges();
  });

  const text = () => (fixture.nativeElement as HTMLElement).textContent!;

  it('follows the live updates while on screen', () => {
    expect(live.follow).toHaveBeenCalledTimes(1);
    fixture.destroy();
    expect(live.release).toHaveBeenCalledTimes(1);
  });

  it('starts from the session snapshot, with the average power until the charger reports it', () => {
    expect(widget.energy).toBe(2);
    expect(widget.cost).toBe(5);
    expect(widget.power).toBe(12); // 2 kWh in 10 minutes
    expect(widget.powerIsAverage).toBeTrue();
    expect(widget.remaining).toBe(95);
    widget.now = Date.now();
    expect(widget.clock).toMatch(/^1[01]:\d\d$/);
  });

  it('shows the pushed energy, power and cost of its own session only', () => {
    updates.next(update({ sessionId: 99, energyKWh: 50 }));
    expect(widget.energy).toBe(2);

    updates.next(update());
    fixture.detectChanges();

    expect(widget.energy).toBe(3);
    expect(widget.power).toBe(22);
    expect(widget.powerIsAverage).toBeFalse();
    expect(widget.cost).toBe(7.5);
    expect(widget.usedPercent).toBe(8);
    expect(text()).toContain('portal.live.connection.live');
  });

  it('never goes back when an older snapshot arrives after a push', () => {
    updates.next(update({ energyKWh: 4, currentCost: 10 }));
    widget.session = session({ chargedKwhs: 3, currentCost: 7 });

    expect(widget.energy).toBe(4);
    expect(widget.cost).toBe(10);
  });

  it('warns when the balance runs out and emits ended when the session is over', () => {
    const ended: LiveSessionUpdate[] = [];
    widget.ended.subscribe(u => ended.push(u));

    updates.next(update({ status: 'StoppingLowBalance' }));
    fixture.detectChanges();
    expect(text()).toContain('portal.live.lowBalance');

    updates.next(update({ status: 'Ended', energyKWh: 6, currentCost: 15 }));
    expect(ended.length).toBe(1);
    expect(ended[0].currentCost).toBe(15);
  });

  it('asks for a confirmation, then stops the session and says so', () => {
    live.stop.and.returnValue(of({ message: 'ok', status: 'Accepted' }));
    const button = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button')).find(b => b.textContent!.includes('portal.live.stop'))!;

    button.click();
    fixture.detectChanges();
    expect(widget.stopDialog!.isOpen).toBeTrue();
    expect(live.stop).not.toHaveBeenCalled();

    (fixture.nativeElement.querySelector('[data-confirm]') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(live.stop).toHaveBeenCalledWith('CP-1', 'TX-5');
    expect(widget.stopping).toBeTrue();
    expect(text()).toContain('portal.command.stop.done');
    expect(widget.canStop).toBeFalse();
  });

  it('keeps the session going and explains why when the charger cannot be reached', () => {
    live.stop.and.returnValue(throwError(() => new HttpErrorResponse({ status: 409, error: { status: 'NotConnected' } })));

    widget.stop();
    fixture.detectChanges();

    expect(widget.stopping).toBeFalse();
    expect(widget.feedback?.key).toBe('portal.command.offline');
    expect(widget.canStop).toBeTrue();
  });

  it('offers no stop without the transaction to stop', () => {
    widget.session = session({ transactionUid: null });

    expect(widget.canStop).toBeFalse();
    widget.stop();
    expect(live.stop).not.toHaveBeenCalled();
  });
});
