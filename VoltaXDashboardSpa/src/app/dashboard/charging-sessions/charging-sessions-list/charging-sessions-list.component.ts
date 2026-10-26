import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { ListFilterDef, changedFrom } from 'src/_models/list-filter';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { saveDownload } from 'src/_helpers/download';
import { OperatorSession, OperatorSessionFilter } from 'src/_models/operator';
import { PagedResult } from 'src/_models/portal';
import { OperatorSearchService } from 'src/_services/operator-search.service';
import { statusTone } from 'src/app/shared/portal-ui/portal-ui.module';

const EMPTY: OperatorSessionFilter = { from: null, to: null, status: '', search: '', page: 1, pageSize: 25 };

/** Every charging session of the network: filters, paging, CSV / PDF export and print. */
@Component({
  selector: 'app-charging-sessions-list',
  templateUrl: './charging-sessions-list.component.html'
})
export class ChargingSessionsListComponent implements OnInit {
  private readonly search = inject(OperatorSearchService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  filter: OperatorSessionFilter = { ...EMPTY };
  /** The filters of the last load, shown as removable chips (the form may hold edits not applied yet). */
  applied: Record<string, any> = {};
  readonly chipDefs: ListFilterDef[] = [
    { key: 'from', label: 'common.fields.from', type: 'date' },
    { key: 'to', label: 'common.fields.to', type: 'date' },
    { key: 'status', label: 'common.fields.status', type: 'select', options: [
      { value: 'active', label: 'customer.status.charging' }, { value: 'completed', label: 'customer.status.Completed' }] },
    { key: 'search', label: 'common.fields.search', type: 'text' }
  ];
  result: PagedResult<OperatorSession> | null = null;
  loading = false;
  error = false;
  exporting: 'csv' | 'pdf' | null = null;
  readonly tone = statusTone;

  ngOnInit(): void {
    const params = this.route.snapshot.queryParamMap;
    this.filter.chargePointId = this.id(params.get('chargePoint'));
    this.filter.userId = this.id(params.get('user'));
    this.filter.stationId = this.id(params.get('station'));
    this.load();
  }

  load(page = 1): void {
    this.filter.page = page;
    this.applied = changedFrom(this.filter, EMPTY);
    this.loading = true;
    this.search.sessions(this.filter).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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

  /** Filters that came from another page (charge point, customer, station) and are not in the form. */
  get scoped(): boolean { return !!(this.filter.chargePointId || this.filter.userId || this.filter.stationId); }

  get totals(): { minutes: number; kwh: number; amount: number } {
    const items = this.result?.items ?? [];
    return {
      minutes: items.reduce((sum, s) => sum + s.chargedMinutes, 0),
      kwh: items.reduce((sum, s) => sum + s.chargedKwhs, 0),
      amount: items.reduce((sum, s) => sum + (s.active ? 0 : s.total), 0)
    };
  }

  get period(): string | null {
    return this.filter.from || this.filter.to ? `${this.filter.from || '…'} → ${this.filter.to || '…'}` : null;
  }

  export(format: 'csv' | 'pdf'): void {
    this.exporting = format;
    this.search.exportSessions(this.filter, format).subscribe({
      next: response => { saveDownload(response, `eveon-sessions.${format}`); this.exporting = null; },
      error: () => { this.exporting = null; this.error = true; }
    });
  }

  print(): void { window.print(); }

  private id(value: string | null): number | null {
    const n = Number(value);
    return Number.isInteger(n) && n > 0 ? n : null;
  }
}
