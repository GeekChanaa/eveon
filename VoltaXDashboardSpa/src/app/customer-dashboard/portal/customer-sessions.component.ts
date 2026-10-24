import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { saveDownload } from 'src/_helpers/download';
import { MySession, MySessionFilter, PagedResult } from 'src/_models/portal';
import { AccessService } from 'src/_services/access.service';
import { LanguageService } from 'src/_services/language.service';
import { CustomerPortalService } from 'src/_services/portal/customer-portal.service';
import { fullName, statusTone } from 'src/app/shared/portal-ui/portal-ui.module';

const EMPTY: MySessionFilter = { from: null, to: null, status: '', search: '', page: 1, pageSize: 20 };

/** The customer's charging history: filters, paging, CSV / PDF export and a printable list. */
@Component({
  selector: 'app-customer-sessions',
  templateUrl: './customer-sessions.component.html'
})
export class CustomerSessionsComponent implements OnInit {
  private readonly portal = inject(CustomerPortalService);
  private readonly access = inject(AccessService);
  private readonly language = inject(LanguageService);
  private readonly destroyRef = inject(DestroyRef);

  filter: MySessionFilter = { ...EMPTY };
  result: PagedResult<MySession> | null = null;
  loading = false;
  error = false;
  exportError = false;
  exporting: 'csv' | 'pdf' | null = null;
  readonly tone = statusTone;

  ngOnInit(): void { this.load(); }

  load(page = 1): void {
    this.filter.page = page;
    this.loading = true;
    this.portal.sessions(this.filter).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => { this.result = result; this.loading = false; this.error = false; },
      error: () => { this.loading = false; this.error = true; }
    });
  }

  reset(): void {
    this.filter = { ...EMPTY };
    this.load();
  }

  get filtered(): boolean { return !!(this.filter.from || this.filter.to || this.filter.status || this.filter.search); }

  get totals(): { kwh: number; amount: number; minutes: number } {
    const items = this.result?.items ?? [];
    return {
      kwh: items.reduce((sum, s) => sum + s.chargedKwhs, 0),
      amount: items.reduce((sum, s) => sum + (s.active ? 0 : s.total), 0),
      minutes: items.reduce((sum, s) => sum + s.chargedMinutes, 0)
    };
  }

  /** The filters in words, printed under the title. */
  get criteria(): string | null {
    const parts: string[] = [];
    if (this.filter.from || this.filter.to)
      parts.push(this.language.t('portal.print.period', { from: this.filter.from || '…', to: this.filter.to || '…' }));
    if (this.filter.status)
      parts.push(this.language.t(this.filter.status === 'active' ? 'customer.status.charging' : 'customer.status.Completed'));
    if (this.filter.search) parts.push(`“${this.filter.search}”`);
    return parts.length ? parts.join(' · ') : null;
  }

  get holder(): string | null { return fullName(this.access.current); }

  export(format: 'csv' | 'pdf'): void {
    this.exporting = format;
    this.exportError = false;
    this.portal.exportSessions(this.filter, format).subscribe({
      next: response => { saveDownload(response, `eveon-sessions.${format}`); this.exporting = null; },
      error: () => { this.exporting = null; this.exportError = true; }
    });
  }

  print(): void { window.print(); }
}
