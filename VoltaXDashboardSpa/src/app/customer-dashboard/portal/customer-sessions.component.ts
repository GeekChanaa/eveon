import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { saveDownload } from 'src/_helpers/download';
import { MySession, MySessionFilter, PagedResult } from 'src/_models/portal';
import { CustomerPortalService } from 'src/_services/portal/customer-portal.service';
import { statusTone } from 'src/app/shared/portal-ui/portal-ui.module';

/** The customer's charging history: filters, paging, CSV / PDF export and print. */
@Component({
  selector: 'app-customer-sessions',
  templateUrl: './customer-sessions.component.html'
})
export class CustomerSessionsComponent implements OnInit {
  private readonly portal = inject(CustomerPortalService);
  private readonly destroyRef = inject(DestroyRef);

  filter: MySessionFilter = { from: null, to: null, status: '', search: '', page: 1, pageSize: 20 };
  result: PagedResult<MySession> | null = null;
  loading = false;
  error = false;
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
    this.filter = { from: null, to: null, status: '', search: '', page: 1, pageSize: 20 };
    this.load();
  }

  get totals(): { kwh: number; amount: number } {
    const items = this.result?.items ?? [];
    return { kwh: items.reduce((sum, s) => sum + s.chargedKwhs, 0), amount: items.reduce((sum, s) => sum + (s.active ? 0 : s.total), 0) };
  }

  export(format: 'csv' | 'pdf'): void {
    this.exporting = format;
    this.portal.exportSessions(this.filter, format).subscribe({
      next: response => { saveDownload(response, `eveon-sessions.${format}`); this.exporting = null; },
      error: () => { this.exporting = null; this.error = true; }
    });
  }

  print(): void { window.print(); }
}
