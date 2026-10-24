import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { VX_TEST_SCHEMAS, VxTestingModule } from 'src/testing/vx-testing.module';
import { RefundSummary } from 'src/_models/billing';
import { AccessService } from 'src/_services/access.service';
import { BillingService } from 'src/_services/billing/billing.service';
import { RefundPanelComponent } from './refund-panel.component';

const summary = (extra: Partial<RefundSummary> = {}): RefundSummary => ({
  target: 'Order', id: 7, paid: 100, refunded: 40, refundable: 60, cardBalance: 80, methods: ['Gateway', 'External'],
  defaultMethod: 'Gateway', provider: 'Stripe', note: null, refunds: [], ...extra
});

describe('RefundPanelComponent', () => {
  let billing: jasmine.SpyObj<BillingService>;
  let canEdit: boolean;
  let panel: RefundPanelComponent;

  beforeEach(async () => {
    billing = jasmine.createSpyObj<BillingService>('BillingService', ['refunds', 'refund']);
    billing.refunds.and.returnValue(of(summary()));
    canEdit = true;
    await TestBed.configureTestingModule({
      imports: [VxTestingModule, RefundPanelComponent],
      providers: [
        { provide: BillingService, useValue: billing },
        { provide: AccessService, useValue: { can: (permission: string) => permission === 'EditRechargeOrders' && canEdit } }
      ],
      schemas: VX_TEST_SCHEMAS
    }).compileComponents();
    const fixture = TestBed.createComponent(RefundPanelComponent);
    panel = fixture.componentInstance;
    panel.target = 'Order';
    panel.targetId = 7;
    panel.ngOnChanges();
    fixture.detectChanges();
  });

  it('shows what is left to refund and offers a refund to operators who may credit wallets', () => {
    expect(billing.refunds).toHaveBeenCalledWith('Order', 7);
    expect(panel.summary?.refundable).toBe(60);
    expect(panel.canRefund).toBeTrue();
    canEdit = false;
    expect(panel.canRefund).toBeFalse();
  });

  it('starts from the whole refundable amount and the default method', () => {
    panel.open();

    expect(panel.step).toBe('form');
    expect(panel.amount).toBe(60);
    expect(panel.method).toBe('Gateway');
  });

  it('refuses more than what is left, fractions of cents and a missing reason', () => {
    panel.open();
    panel.reason = 'Faulty charger';
    panel.amount = 60.01;
    panel.review();
    expect(panel.formError).toBe('billing.refunds.amountRange');
    panel.amount = 10.005;
    panel.review();
    expect(panel.formError).toBe('billing.refunds.amountCents');
    panel.amount = 10;
    panel.reason = ' ';
    panel.review();
    expect(panel.formError).toBe('billing.refunds.reasonRequired');
    expect(billing.refund).not.toHaveBeenCalled();
  });

  it('asks for confirmation, then refunds with one idempotency key and reloads', () => {
    billing.refund.and.returnValue(of({ id: 3, amount: 25, creditNoteNumber: 'AV-2026-000004' } as any));
    panel.open();
    panel.amount = 25;
    panel.reason = 'Customer request';
    panel.review();
    expect(panel.step).toBe('confirm');

    panel.submit();

    const [target, id, request] = billing.refund.calls.mostRecent().args;
    expect([target, id]).toEqual(['Order', 7]);
    expect(request.amount).toBe(25);
    expect(request.method).toBe('Gateway');
    expect(request.idempotencyKey).toMatch(/^[0-9a-f]{32}$/);
    expect(panel.done?.creditNoteNumber).toBe('AV-2026-000004');
    expect(panel.step).toBe('idle');
    expect(billing.refunds).toHaveBeenCalledTimes(2);
  });

  it('retries a refund lost on the network with the same key', () => {
    billing.refund.and.returnValues(throwError(() => ({ status: 0 })), of({ id: 3, amount: 25 } as any));
    panel.open();
    panel.amount = 25;
    panel.reason = 'Customer request';
    panel.review();

    panel.submit();
    expect(panel.error).toBeTruthy();
    panel.submit();

    const keys = billing.refund.calls.all().map(c => c.args[2].idempotencyKey);
    expect(keys[0]).toBe(keys[1]);
  });

  it('shows why the API refused the refund', () => {
    billing.refund.and.returnValue(throwError(() => ({ status: 422, error: { code: 'insufficient_balance', error: 'The wallet holds 5.00 MAD.' } })));
    panel.open();
    panel.amount = 25;
    panel.reason = 'Customer request';
    panel.review();

    panel.submit();

    expect(panel.error).toBe('The wallet holds 5.00 MAD.');
    expect(panel.step).toBe('form');
  });

  it('explains when a top-up can only be refunded outside the platform', () => {
    panel.summary = summary({ methods: ['External'], defaultMethod: 'External', provider: 'Manual' });
    expect(panel.noteKey).toBe('billing.refunds.notes.manual');
    panel.summary = summary({ methods: ['External'], defaultMethod: 'External', provider: 'Cmi' });
    expect(panel.noteKey).toBe('billing.refunds.notes.provider');
    panel.summary = summary();
    expect(panel.noteKey).toBeNull();
  });
});
