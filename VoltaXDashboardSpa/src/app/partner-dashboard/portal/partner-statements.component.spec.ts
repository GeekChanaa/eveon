import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';
import { VX_TEST_SCHEMAS, VxTestingModule } from 'src/testing/vx-testing.module';
import { Settlement, SettlementDetail, SettlementLine } from 'src/_models/billing';
import { PartnerStatementsService } from 'src/_services/billing/billing.service';
import { PartnerStatementsComponent } from './partner-statements.component';

const settlement: Settlement = {
  id: 5, partnerId: 2, partnerName: 'Alpha', year: 2026, month: 9, periodStart: '2026-08-31T23:00:00Z', periodEnd: '2026-09-30T23:00:00Z',
  sessionCount: 2, energyKwh: 22.345, grossAmount: 48, netExclVat: 40, revenueVat: 8, commissionExclVat: 8.47, commissionVat: 1.69,
  netPayout: 37.84, commissionPercent: 10, commissionPerKwh: 0.2, vatRate: 0.2, status: 'Pending', paidAt: null, paymentReference: null,
  generatedAt: '2026-10-02T00:00:00Z'
};
const line = (id: number, gross: number, payout: number, extra: Partial<SettlementLine> = {}): SettlementLine => ({
  id, sourceKey: 'session:' + id, chargingSessionId: id, refundId: null, date: '2026-09-10T08:00:00Z', station: 'Anfa', chargePoint: 'CP-1',
  kwh: 10, gross, netExclVat: gross / 1.2, vat: gross - gross / 1.2, commissionExclVat: 1, commissionVat: 0.2, netPayout: payout, ...extra
});

describe('PartnerStatementsComponent', () => {
  let statements: jasmine.SpyObj<PartnerStatementsService>;
  let params: BehaviorSubject<any>;
  let page: PartnerStatementsComponent;

  beforeEach(async () => {
    statements = jasmine.createSpyObj<PartnerStatementsService>('PartnerStatementsService', ['list', 'detail', 'export']);
    statements.list.and.returnValue(of({ items: [settlement], page: 1, pageSize: 24, total: 1 }));
    params = new BehaviorSubject(convertToParamMap({}));
    await TestBed.configureTestingModule({
      imports: [VxTestingModule, PartnerStatementsComponent],
      providers: [
        { provide: PartnerStatementsService, useValue: statements },
        { provide: ActivatedRoute, useValue: { paramMap: params } }
      ],
      schemas: VX_TEST_SCHEMAS
    }).compileComponents();
    const fixture = TestBed.createComponent(PartnerStatementsComponent);
    page = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('lists the partner statements', () => {
    expect(statements.list).toHaveBeenCalledWith(null, 1);
    expect(page.result?.items.length).toBe(1);
    expect(page.commission(settlement)).toBeCloseTo(10.16, 6);
    expect(page.period(settlement)).toEqual(new Date(2026, 8, 1));
  });

  it('opens a statement from the route with every line, whose totals are the statement totals', () => {
    const detail: SettlementDetail = { settlement, lines: [line(1, 34, 27.64), line(2, 20, 15.6), line(3, -6, -5.4, { refundId: 9, kwh: 0 })] };
    statements.detail.and.returnValue(of(detail));

    params.next(convertToParamMap({ id: '5' }));

    expect(statements.detail).toHaveBeenCalledWith(5);
    expect(page.detail).toBe(detail);
    expect(page.lineTotals.gross).toBe(48);
    expect(page.lineTotals.payout).toBeCloseTo(37.84, 6);
    expect(page.lineTotals.kwh).toBe(20);
    expect(page.isRefund(detail.lines[2])).toBeTrue();
  });

  it('exports a statement in the interface language', () => {
    statements.export.and.returnValue(of(null as any));
    spyOn(window.URL, 'createObjectURL').and.returnValue('blob:x');
    statements.export.and.returnValue(of({ headers: { get: () => null }, body: new Blob() } as any));

    page.export(settlement, 'pdf');

    expect(statements.export).toHaveBeenCalledWith(5, 'pdf', 'fr');
    expect(page.exporting).toBeNull();
  });
});
