import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { VX_TEST_SCHEMAS, VxTestingModule } from 'src/testing/vx-testing.module';
import { Settlement } from 'src/_models/billing';
import { AccessService } from 'src/_services/access.service';
import { BillingService } from 'src/_services/billing/billing.service';
import { SettlementsPageComponent } from './settlements-page.component';

const settlement = { id: 5, partnerId: 2, partnerName: 'Alpha', year: 2026, month: 9, grossAmount: 54, commissionExclVat: 8.97, commissionVat: 1.79,
  netPayout: 43.24, status: 'Pending' } as Settlement;

describe('SettlementsPageComponent', () => {
  let billing: jasmine.SpyObj<BillingService>;
  let page: SettlementsPageComponent;

  beforeEach(async () => {
    billing = jasmine.createSpyObj<BillingService>('BillingService', ['settlements', 'commissions', 'updateCommission', 'generate', 'markPaid', 'settlement']);
    billing.settlements.and.returnValue(of({ items: [settlement], page: 1, pageSize: 25, total: 1 }));
    billing.commissions.and.returnValue(of([{ partnerId: 2, name: 'Alpha', commissionPercent: 10, commissionPerKwh: 0.2 }]));
    await TestBed.configureTestingModule({
      imports: [VxTestingModule, SettlementsPageComponent],
      providers: [{ provide: BillingService, useValue: billing }, { provide: AccessService, useValue: { can: () => true } }],
      schemas: VX_TEST_SCHEMAS
    }).compileComponents();
    const fixture = TestBed.createComponent(SettlementsPageComponent);
    page = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('lists settlements and the commission terms of every partner', () => {
    expect(page.result?.total).toBe(1);
    expect(page.terms[0]).toEqual(jasmine.objectContaining({ partnerId: 2, percent: 10, perKwh: 0.2 }));
    expect(page.pageTotals.commission).toBeCloseTo(10.76, 6);
  });

  it('generates last month by default, for every partner', () => {
    billing.generate.and.returnValue(of([settlement]));
    const last = new Date();
    last.setDate(1);
    last.setMonth(last.getMonth() - 1);

    page.generate();

    expect(billing.generate).toHaveBeenCalledWith(last.getFullYear(), last.getMonth() + 1, null);
    expect(page.filter.month).toBe(last.getMonth() + 1);
  });

  it('records a payout only with a reference', () => {
    billing.markPaid.and.returnValue(of({ ...settlement, status: 'Paid' }));
    page.startPayment(settlement);
    page.confirmPayment();
    expect(billing.markPaid).not.toHaveBeenCalled();

    page.payReference = 'VIR-778';
    page.payDate = '2026-10-05';
    page.confirmPayment();

    expect(billing.markPaid).toHaveBeenCalledWith(5, 'VIR-778', new Date('2026-10-05T12:00:00').toISOString());
    expect(page.paying).toBeNull();
  });

  it('checks the commission terms before saving them', () => {
    const row = page.terms[0];
    row.percent = 150;
    page.saveTerms(row);
    expect(billing.updateCommission).not.toHaveBeenCalled();

    billing.updateCommission.and.returnValue(of({ partnerId: 2, name: 'Alpha', commissionPercent: 12.5, commissionPerKwh: 0.1 }));
    row.percent = 12.5;
    row.perKwh = 0.1;
    page.saveTerms(row);

    expect(billing.updateCommission).toHaveBeenCalledWith(2, 12.5, 0.1);
    expect(row.saved).toBeTrue();
  });
});
