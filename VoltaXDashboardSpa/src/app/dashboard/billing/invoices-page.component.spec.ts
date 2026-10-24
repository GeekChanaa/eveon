import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { VX_TEST_SCHEMAS, VxTestingModule } from 'src/testing/vx-testing.module';
import { BillingService } from 'src/_services/billing/billing.service';
import { InvoicesPageComponent } from './invoices-page.component';

describe('InvoicesPageComponent', () => {
  it('counts credit notes negative in the page totals', async () => {
    const billing = jasmine.createSpyObj<BillingService>('BillingService', ['invoices']);
    billing.invoices.and.returnValue(of({ page: 1, pageSize: 25, total: 2, items: [
      { id: 1, kind: 'Invoice', number: 'EV-2026-000001', amountExclVat: 100, vatAmount: 20, amountInclVat: 120 } as any,
      { id: 2, kind: 'CreditNote', number: 'AV-2026-000001', amountExclVat: 25, vatAmount: 5, amountInclVat: 30 } as any
    ] }));
    await TestBed.configureTestingModule({
      imports: [VxTestingModule, InvoicesPageComponent],
      providers: [{ provide: BillingService, useValue: billing }],
      schemas: VX_TEST_SCHEMAS
    }).compileComponents();
    const fixture = TestBed.createComponent(InvoicesPageComponent);
    fixture.detectChanges();

    expect(fixture.componentInstance.totals).toEqual({ exclVat: 75, vat: 15, inclVat: 90 });
  });
});
