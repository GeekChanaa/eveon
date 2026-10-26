import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { ListFilterDef, changedFrom } from 'src/_models/list-filter';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { saveDownload } from 'src/_helpers/download';
import { OperatorTransaction, OperatorTransactionFilter } from 'src/_models/operator';
import { PagedResult } from 'src/_models/portal';
import { OperatorSearchService } from 'src/_services/operator-search.service';

const EMPTY: OperatorTransactionFilter = { from: null, to: null, status: '', search: '', page: 1, pageSize: 25 };

/** Every OCPP transaction of the network with its meter readings: filters, paging, CSV export and print. */
@Component({
  selector: 'app-transactions-list',
  templateUrl: './transactions-list.component.html'
})
export class TransactionsListComponent implements OnInit {
  private readonly search = inject(OperatorSearchService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  filter: OperatorTransactionFilter = { ...EMPTY };
  /** The filters of the last load, shown as removable chips (the form may hold edits not applied yet). */
  applied: Record<string, any> = {};
  readonly chipDefs: ListFilterDef[] = [
    { key: 'from', label: 'common.fields.from', type: 'date' },
    { key: 'to', label: 'common.fields.to', type: 'date' },
    { key: 'status', label: 'common.fields.status', type: 'select', options: [
      { value: 'Current', label: 'ops.transactionStatus.Current' }, { value: 'Ended', label: 'ops.transactionStatus.Ended' },
      { value: 'Forbidden', label: 'ops.transactionStatus.Forbidden' }] },
    { key: 'search', label: 'common.fields.search', type: 'text' }
  ];
  result: PagedResult<OperatorTransaction> | null = null;
  loading = false;
  error = false;
  exporting = false;

  ngOnInit(): void {
    const chargePoint = Number(this.route.snapshot.queryParamMap.get('chargePoint'));
    this.filter.chargePointId = Number.isInteger(chargePoint) && chargePoint > 0 ? chargePoint : null;
    this.load();
  }

  load(page = 1): void {
    this.filter.page = page;
    this.applied = changedFrom(this.filter, EMPTY);
    this.loading = true;
    this.search.transactions(this.filter).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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

  /** Energy of the transaction in kWh (meters are in Wh), null while it runs. */
  energy(t: OperatorTransaction): number | null {
    return t.meterStop == null ? null : Math.max(0, t.meterStop - t.meterStart) / 1000;
  }

  tone(status: string | null): string {
    return status === 'Current' ? 'vx-badge--success' : status === 'Forbidden' ? 'vx-badge--danger' : 'vx-badge--info';
  }

  get period(): string | null {
    return this.filter.from || this.filter.to ? `${this.filter.from || '…'} → ${this.filter.to || '…'}` : null;
  }

  export(): void {
    this.exporting = true;
    this.search.exportTransactions(this.filter).subscribe({
      next: response => { saveDownload(response, 'eveon-transactions.csv'); this.exporting = false; },
      error: () => { this.exporting = false; this.error = true; }
    });
  }

  print(): void { window.print(); }
}
