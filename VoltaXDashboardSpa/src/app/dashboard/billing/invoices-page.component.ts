import { Component, OnInit } from '@angular/core';
import { FilterChipsComponent } from 'src/app/atoms/filter-chips/filter-chips.component';
import { ListFilterDef, changedFrom } from 'src/_models/list-filter';
import { saveDownload } from 'src/_helpers/download';
import { PagedResult } from 'src/_models/portal';
import { InvoiceFilter, InvoiceRow } from 'src/_models/billing';
import { BillingService } from 'src/_services/billing/billing.service';
import { PortalUiModule } from 'src/app/shared/portal-ui/portal-ui.module';

const EMPTY: InvoiceFilter = { from: null, to: null, kind: '', search: null, page: 1, pageSize: 25 };

/** The register of issued invoices and credit notes: search, stored PDFs and the CSV journal for accounting. */
@Component({
  selector: 'app-invoices-page',
  standalone: true,
  imports: [PortalUiModule, FilterChipsComponent],
  templateUrl: './invoices-page.component.html'
})
export class InvoicesPageComponent implements OnInit {
  filter: InvoiceFilter = { ...EMPTY };
  /** The filters of the last load, shown as removable chips (the form may hold edits not applied yet). */
  applied: Record<string, any> = {};
  readonly chipDefs: ListFilterDef[] = [
    { key: 'from', label: 'common.fields.from', type: 'date' },
    { key: 'to', label: 'common.fields.to', type: 'date' },
    { key: 'kind', label: 'billing.invoices.kind', type: 'select', options: [
      { value: 'Invoice', label: 'billing.invoices.kinds.Invoice' }, { value: 'CreditNote', label: 'billing.invoices.kinds.CreditNote' }] },
    { key: 'search', label: 'common.fields.search', type: 'text' }
  ];
  result: PagedResult<InvoiceRow> | null = null;
  loading = false;
  error = false;
  downloading = false;

  constructor(private billing: BillingService) {}

  ngOnInit(): void { this.load(); }

  load(page = 1): void {
    this.filter.page = page;
    this.applied = changedFrom(this.filter, EMPTY);
    this.loading = true;
    this.billing.invoices(this.filter).subscribe({
      next: result => { this.result = result; this.loading = false; this.error = false; },
      error: () => { this.loading = false; this.error = true; }
    });
  }

  reset(): void {
    this.filter = { ...EMPTY };
    this.load();
  }

  /** A chip was removed: that filter goes back to empty and the list reloads. */
  removeFilter(key: string): void {
    (this.filter as any)[key] = (EMPTY as any)[key];
    this.load();
  }

  /** Credit notes count negative in the totals, as in the journal. */
  signed(row: InvoiceRow, amount: number): number { return row.kind === 'CreditNote' ? -amount : amount; }

  get totals(): { exclVat: number; vat: number; inclVat: number } {
    const items = this.result?.items ?? [];
    return {
      exclVat: items.reduce((sum, r) => sum + this.signed(r, r.amountExclVat), 0),
      vat: items.reduce((sum, r) => sum + this.signed(r, r.vatAmount), 0),
      inclVat: items.reduce((sum, r) => sum + this.signed(r, r.amountInclVat), 0)
    };
  }

  pdf(row: InvoiceRow): void {
    this.downloading = true;
    this.billing.invoicePdf(row.id).subscribe({
      next: response => { saveDownload(response, `${row.number}.pdf`); this.downloading = false; },
      error: () => { this.downloading = false; this.error = true; }
    });
  }

  journal(): void {
    this.downloading = true;
    this.billing.invoiceJournal(this.filter).subscribe({
      next: response => { saveDownload(response, 'eveon-invoices.csv'); this.downloading = false; },
      error: () => { this.downloading = false; this.error = true; }
    });
  }
}
