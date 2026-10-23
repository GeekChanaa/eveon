import { TestBed } from '@angular/core/testing';
import { HttpTestingController } from '@angular/common/http/testing';
import { VxTestingModule } from 'src/testing/vx-testing.module';
import { environment } from 'src/environments/environment';
import { BillingService, PartnerStatementsService } from './billing.service';

describe('BillingService', () => {
  let http: HttpTestingController;
  let billing: BillingService;
  let statements: PartnerStatementsService;
  const api = `${environment.apiUrl}/api`;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [VxTestingModule] });
    http = TestBed.inject(HttpTestingController);
    billing = TestBed.inject(BillingService);
    statements = TestBed.inject(PartnerStatementsService);
  });

  afterEach(() => http.verify());

  it('posts a refund with its idempotency key', () => {
    billing.refund('Session', 12, { amount: 5, reason: 'Faulty connector', idempotencyKey: 'k'.repeat(32), method: 'Wallet' }).subscribe();

    const request = http.expectOne(`${api}/Refund/Session/12`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body.idempotencyKey).toBe('k'.repeat(32));
    request.flush({});
  });

  it('reads the refund summary of an order', () => {
    billing.refunds('Order', 7).subscribe();
    http.expectOne(`${api}/Refund/Order/7`).flush({});
  });

  it('searches invoices without empty filters and exports the journal without paging', () => {
    billing.invoices({ from: '2026-10-01', to: null, kind: '', search: 'EV-', page: 2, pageSize: 25 }).subscribe();
    billing.invoiceJournal({ from: '2026-10-01', page: 2, pageSize: 25 }).subscribe();

    const search = http.expectOne(r => r.url === `${api}/Invoice/Search`);
    expect(search.request.params.keys().sort()).toEqual(['from', 'page', 'pageSize', 'search']);
    search.flush({ items: [], page: 2, pageSize: 25, total: 0 });
    const journal = http.expectOne(r => r.url === `${api}/Invoice/Journal`);
    expect(journal.request.params.keys()).toEqual(['from']);
    journal.flush(new Blob());
  });

  it('generates settlements, records payouts and changes commission terms', () => {
    billing.generate(2026, 9).subscribe();
    billing.markPaid(5, 'VIR-1').subscribe();
    billing.updateCommission(2, 12.5, 0.1).subscribe();

    expect(http.expectOne(`${api}/Settlement/Generate`).request.body).toEqual({ year: 2026, month: 9, partnerId: null });
    expect(http.expectOne(`${api}/Settlement/5/Paid`).request.body).toEqual({ reference: 'VIR-1', paidAt: null });
    const terms = http.expectOne(`${api}/Settlement/Commissions/2`);
    expect(terms.request.method).toBe('PUT');
    expect(terms.request.body).toEqual({ commissionPercent: 12.5, commissionPerKwh: 0.1 });
  });

  it('reads the partner portal statements', () => {
    statements.list(2026).subscribe();
    statements.export(5, 'csv', 'ar').subscribe();

    const list = http.expectOne(r => r.url === `${api}/partner-portal/statements`);
    expect(list.request.params.get('year')).toBe('2026');
    list.flush({ items: [], page: 1, pageSize: 24, total: 0 });
    const exported = http.expectOne(r => r.url === `${api}/partner-portal/statements/5/export`);
    expect(exported.request.params.get('format')).toBe('csv');
    expect(exported.request.params.get('language')).toBe('ar');
    exported.flush(new Blob());
  });
});
