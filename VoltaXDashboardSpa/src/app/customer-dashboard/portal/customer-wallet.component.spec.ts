import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { configureVxTest } from 'src/testing/vx-testing.module';
import { MyCard } from 'src/_models/portal';
import { CustomerPortalService } from 'src/_services/portal/customer-portal.service';
import { PaymentsService } from 'src/_services/portal/payments.service';
import { CustomerWalletComponent } from './customer-wallet.component';

const card = (id: number, balance: number, extra: Partial<MyCard> = {}): MyCard => ({
  id, maskedNumber: '•••• ' + id, last4: String(id), balance, expirationDate: '2030-01-01', status: 'Active', blocked: false, expired: false, ...extra
});

describe('CustomerWalletComponent', () => {
  let payments: jasmine.SpyObj<PaymentsService>;
  let wallet: CustomerWalletComponent;

  beforeEach(async () => {
    const portal = jasmine.createSpyObj<CustomerPortalService>('CustomerPortalService', ['cards', 'orders', 'wallet', 'orderInvoice']);
    portal.cards.and.returnValue(of([card(1, 20, { blocked: true }), card(2, 35.5), card(3, 10, { expired: true })]));
    portal.orders.and.returnValue(of({ items: [], page: 1, pageSize: 5, total: 0 }));
    portal.wallet.and.returnValue(of({ items: [], page: 1, pageSize: 10, total: 0 }));
    payments = jasmine.createSpyObj<PaymentsService>('PaymentsService', ['config', 'startTopUp', 'topUp']);
    payments.config.and.returnValue(of({ provider: 'Sandbox', enabled: true, currency: 'MAD', minTopUp: 20, maxTopUp: 5000, supportsSavedCards: true }));
    await configureVxTest([CustomerWalletComponent], {
      providers: [{ provide: CustomerPortalService, useValue: portal }, { provide: PaymentsService, useValue: payments }]
    });
    const fixture = TestBed.createComponent(CustomerWalletComponent);
    wallet = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('shows the total balance and preselects the first card that can be topped up', () => {
    expect(wallet.balance).toBe(65.5);
    expect(wallet.usableCards.map(c => c.id)).toEqual([2]);
    expect(wallet.cardId).toBe(2);
  });

  it('checks the amount against the provider limits and cents', () => {
    wallet.amount = 19.99;
    expect(wallet.amountError).toBe('customer.wallet.amountRange');
    wallet.amount = 5000.01;
    expect(wallet.amountError).toBe('customer.wallet.amountRange');
    wallet.amount = 50.123;
    expect(wallet.amountError).toBe('customer.wallet.amountDecimals');
    wallet.amount = 50.12;
    expect(wallet.amountError).toBeNull();
  });

  it('does not start a payment for an invalid amount', () => {
    wallet.amount = 5;
    wallet.topUp();

    expect(payments.startTopUp).not.toHaveBeenCalled();
  });

  it('shows the API error when the payment cannot start', () => {
    payments.startTopUp.and.returnValue(throwError(() => ({ status: 503, error: { error: 'Online payment is not available.' } })));
    wallet.amount = 100;
    wallet.topUp();

    expect(payments.startTopUp).toHaveBeenCalledWith(100, 2);
    expect(wallet.starting).toBeFalse();
    expect(wallet.startError).toBe('Online payment is not available.');
  });
});
