import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { BehaviorSubject, of, throwError } from 'rxjs';
import { configureVxTest } from 'src/testing/vx-testing.module';
import { MyActiveSession, MyOverview, MySession, MySessionDetail } from 'src/_models/portal';
import { CustomerPortalService } from 'src/_services/portal/customer-portal.service';
import { PortalUiModule } from 'src/app/shared/portal-ui/portal-ui.module';
import { CustomerOverviewComponent } from './customer-overview.component';
import { CustomerSessionDetailComponent } from './customer-session-detail.component';

const active: MyActiveSession = {
  id: 9, stationName: 'Marina', chargePointName: 'CP-1', connector: 'EVSE 1', startDate: '2026-10-23T09:00:00', chargedMinutes: 12,
  chargedKwhs: 4, currentCost: 8, transactionUid: 'TX-9', cardBalance: 60
};

const overview = (activeSession: MyActiveSession | null = null): MyOverview => ({
  firstName: 'Salma', walletBalance: 60, cardCount: 1, activeSession, sessionsThisMonth: 3, kwhThisMonth: 40, spentThisMonth: 90,
  sessionsTotal: 10, kwhTotal: 150, spentTotal: 320, currency: 'MAD'
});

const sessionRow = (id: number, extra: Partial<MySession> = {}): MySession => ({
  id, stationName: 'Marina', stationAddress: null, chargePointName: 'CP-1', connector: 'EVSE 1', startDate: '2026-10-20T09:00:00',
  endDate: '2026-10-20T09:30:00', chargedMinutes: 30, idleMinutes: 0, chargedKwhs: 12, energyAmount: 30, timeAmount: 0, flatFeeAmount: 0,
  idleAmount: 0, total: 30, stoppedReason: 'EVDisconnected', status: 'Completed', active: false, invoiceAvailable: true, ...extra
});

describe('Customer overview and live session pages', () => {
  let portal: jasmine.SpyObj<CustomerPortalService>;

  beforeEach(() => {
    portal = jasmine.createSpyObj<CustomerPortalService>('CustomerPortalService',
      ['overview', 'sessions', 'session', 'activeSession', 'sessionInvoice', 'exportSessions']);
    portal.sessions.and.returnValue(of({ items: [sessionRow(1), sessionRow(2, { active: true, total: 0 })], page: 1, pageSize: 5, total: 2 }));
  });

  describe('CustomerOverviewComponent', () => {
    beforeEach(() => configureVxTest([CustomerOverviewComponent], { imports: [PortalUiModule], providers: [{ provide: CustomerPortalService, useValue: portal }] }));

    it('shows the wallet, the month and the running session live', fakeAsync(() => {
      portal.overview.and.returnValue(of(overview(active)));
      const fixture = TestBed.createComponent(CustomerOverviewComponent);
      fixture.detectChanges();
      tick();
      fixture.detectChanges();
      const element: HTMLElement = fixture.nativeElement;

      expect(element.textContent).toContain('customer.overview.hello');
      expect(element.querySelectorAll('.vx-stat').length).toBe(4);
      expect(element.querySelector('app-customer-live-session')).not.toBeNull();
      expect(element.querySelectorAll('tbody tr').length).toBe(2);
      fixture.destroy();
    }));

    it('reloads the balance and history as soon as the session ends, and sums it up', fakeAsync(() => {
      portal.overview.and.returnValue(of(overview(active)));
      const fixture = TestBed.createComponent(CustomerOverviewComponent);
      fixture.detectChanges();
      tick();
      portal.overview.and.returnValue(of(overview(null)));

      fixture.componentInstance.onEnded({ sessionId: 9, transactionUid: 'TX-9', status: 'Ended', energyKWh: 6, powerKw: null, durationMinutes: 20,
        currentCost: 12, cardBalance: 48, stopReason: 'Remote', timestamp: '2026-10-23T09:20:00Z' });
      tick();
      fixture.detectChanges();

      expect(portal.overview).toHaveBeenCalledTimes(2);
      expect(fixture.componentInstance.overview?.activeSession).toBeNull();
      expect((fixture.nativeElement as HTMLElement).textContent).toContain('portal.live.ended');
      expect((fixture.nativeElement as HTMLElement).querySelector('app-customer-live-session')).toBeNull();
      fixture.destroy();
    }));

    it('says so and offers to retry when the account cannot be loaded', fakeAsync(() => {
      portal.overview.and.returnValue(throwError(() => new Error('down')));
      const fixture = TestBed.createComponent(CustomerOverviewComponent);
      fixture.detectChanges();
      tick();
      fixture.detectChanges();

      expect(fixture.componentInstance.error).toBeTrue();
      expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent).toContain('common.actions.retry');
      fixture.destroy();
    }));
  });

  describe('CustomerSessionDetailComponent', () => {
    const detail = (session: MySession): MySessionDetail => ({
      session, pricePerKWh: 2.5, pricePerMinute: 0, pricePerIdleMinute: 0, flatFee: 0, vatRate: 0.2, totalExclVat: 25, vat: 5,
      balanceBefore: 90, balanceAfter: 60, cardLast4: '0001', currency: 'MAD'
    });

    function create(session: MySession) {
      portal.session.and.returnValue(of(detail(session)));
      const fixture = TestBed.createComponent(CustomerSessionDetailComponent);
      fixture.detectChanges();
      return fixture;
    }

    beforeEach(() => configureVxTest([CustomerSessionDetailComponent], {
      imports: [PortalUiModule],
      providers: [
        { provide: CustomerPortalService, useValue: portal },
        { provide: ActivatedRoute, useValue: { paramMap: new BehaviorSubject(convertToParamMap({ id: '9' })) } }
      ]
    }));

    it('shows the live widget while the session is the one charging', () => {
      portal.activeSession.and.returnValue(of(active));

      const fixture = create(sessionRow(9, { active: true, endDate: null }));
      fixture.detectChanges();

      expect(portal.session).toHaveBeenCalledWith(9);
      expect(fixture.componentInstance.live?.id).toBe(9);
      expect((fixture.nativeElement as HTMLElement).querySelector('app-customer-live-session')).not.toBeNull();
      fixture.destroy();
    });

    it('prints an ended session as a receipt, without live widget', () => {
      const fixture = create(sessionRow(9));

      expect(portal.activeSession).not.toHaveBeenCalled();
      expect((fixture.nativeElement as HTMLElement).querySelector('vx-print-header')).not.toBeNull();
      expect((fixture.nativeElement as HTMLElement).textContent).toContain('customer.session.totalPaid');
    });

    it('reloads the session when it ends on screen', () => {
      portal.activeSession.and.returnValue(of(active));
      const fixture = create(sessionRow(9, { active: true, endDate: null }));
      portal.session.and.returnValue(of(detail(sessionRow(9))));

      fixture.componentInstance.onEnded();

      expect(portal.session).toHaveBeenCalledTimes(2);
      expect(fixture.componentInstance.live).toBeNull();
      fixture.destroy();
    });
  });

});
