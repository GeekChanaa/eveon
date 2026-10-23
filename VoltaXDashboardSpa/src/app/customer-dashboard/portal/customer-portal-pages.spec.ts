import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { configureVxTest } from 'src/testing/vx-testing.module';
import { MySession, PaymentMethod } from 'src/_models/portal';
import { CustomerPortalService } from 'src/_services/portal/customer-portal.service';
import { PaymentsService } from 'src/_services/portal/payments.service';
import { PortalUiModule } from 'src/app/shared/portal-ui/portal-ui.module';
import { CustomerPaymentMethodsComponent } from './customer-payment-methods.component';
import { CustomerSessionsComponent } from './customer-sessions.component';

const sessionRow = (id: number, extra: Partial<MySession> = {}): MySession => ({
  id, stationName: 'Marina', stationAddress: null, chargePointName: 'CP-1', connector: 'EVSE 1', startDate: '2026-10-20T09:00:00',
  endDate: '2026-10-20T09:30:00', chargedMinutes: 30, idleMinutes: 0, chargedKwhs: 12, energyAmount: 30, timeAmount: 0, flatFeeAmount: 0,
  idleAmount: 0, total: 30, stoppedReason: 'EVDisconnected', status: 'Completed', active: false, invoiceAvailable: true, ...extra
});

describe('Customer portal pages', () => {
  let portal: jasmine.SpyObj<CustomerPortalService>;

  beforeEach(() => {
    portal = jasmine.createSpyObj<CustomerPortalService>('CustomerPortalService',
      ['sessions', 'exportSessions']);
    portal.sessions.and.returnValue(of({ items: [sessionRow(1), sessionRow(2, { active: true, total: 0 })], page: 1, pageSize: 5, total: 2 }));
  });

  describe('CustomerSessionsComponent', () => {
    beforeEach(() => configureVxTest([CustomerSessionsComponent], { imports: [PortalUiModule], providers: [{ provide: CustomerPortalService, useValue: portal }] }));

    it('sums the page, labels cells for phones and only resets when filtered', () => {
      const fixture = TestBed.createComponent(CustomerSessionsComponent);
      fixture.detectChanges();
      const page = fixture.componentInstance;

      expect(page.totals).toEqual({ kwh: 24, amount: 30, minutes: 60 });
      expect((fixture.nativeElement as HTMLElement).querySelector('td[data-label]')).not.toBeNull();
      expect(page.filtered).toBeFalse();
      expect(page.criteria).toBeNull();

      page.filter.status = 'active';
      page.filter.search = 'Marina';
      expect(page.filtered).toBeTrue();
      expect(page.criteria).toContain('“Marina”');
    });

    it('tells when an export fails', () => {
      portal.exportSessions.and.returnValue(throwError(() => new Error('down')));
      const fixture = TestBed.createComponent(CustomerSessionsComponent);
      fixture.detectChanges();

      fixture.componentInstance.export('pdf');

      expect(fixture.componentInstance.exportError).toBeTrue();
      expect(fixture.componentInstance.exporting).toBeNull();
    });
  });

  describe('CustomerPaymentMethodsComponent', () => {
    let payments: jasmine.SpyObj<PaymentsService>;
    const method: PaymentMethod = { id: 3, brand: 'Visa', last4: '4242', expiryMonth: 4, expiryYear: 2031, isDefault: false, provider: 'Sandbox' };

    beforeEach(async () => {
      payments = jasmine.createSpyObj<PaymentsService>('PaymentsService', ['config', 'methods', 'remove', 'setDefault', 'startCardSetup']);
      payments.config.and.returnValue(of({ provider: 'Sandbox', enabled: true, currency: 'MAD', minTopUp: 20, maxTopUp: 5000, supportsSavedCards: true }));
      payments.methods.and.returnValue(of([method]));
      payments.remove.and.returnValue(of(undefined));
      await configureVxTest([CustomerPaymentMethodsComponent], { imports: [PortalUiModule], providers: [{ provide: PaymentsService, useValue: payments }] });
    });

    it('removes a saved card only after the confirmation dialog, never with the browser prompt', () => {
      const confirmSpy = spyOn(window, 'confirm');
      const fixture = TestBed.createComponent(CustomerPaymentMethodsComponent);
      fixture.detectChanges();
      const page = fixture.componentInstance;

      page.askRemove(method);
      fixture.detectChanges();
      expect(page.removeDialog!.isOpen).toBeTrue();
      expect(payments.remove).not.toHaveBeenCalled();

      (fixture.nativeElement.querySelector('[data-confirm]') as HTMLButtonElement).click();

      expect(payments.remove).toHaveBeenCalledWith(3);
      expect(confirmSpy).not.toHaveBeenCalled();
    });

    it('keeps the card when the removal is cancelled', () => {
      const fixture = TestBed.createComponent(CustomerPaymentMethodsComponent);
      fixture.detectChanges();
      fixture.componentInstance.askRemove(method);
      fixture.componentInstance.removeDialog!.cancel();

      expect(fixture.componentInstance.pendingRemoval).toBeNull();
      expect(payments.remove).not.toHaveBeenCalled();
    });
  });
});
