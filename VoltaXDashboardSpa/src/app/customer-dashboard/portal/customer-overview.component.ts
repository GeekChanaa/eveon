import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { interval, startWith, switchMap } from 'rxjs';
import { MyOverview, MySession } from 'src/_models/portal';
import { CustomerPortalService } from 'src/_services/portal/customer-portal.service';
import { statusTone } from 'src/app/shared/portal-ui/portal-ui.module';

/** Customer home: wallet, this month's charging, the live session and the latest sessions. */
@Component({
  selector: 'app-customer-overview',
  templateUrl: './customer-overview.component.html'
})
export class CustomerOverviewComponent implements OnInit {
  private readonly portal = inject(CustomerPortalService);
  private readonly destroyRef = inject(DestroyRef);

  overview: MyOverview | null = null;
  recent: MySession[] = [];
  loading = true;
  error = false;
  readonly tone = statusTone;

  ngOnInit(): void {
    // The live session cost changes while charging: refresh every 30 seconds.
    interval(30_000).pipe(startWith(0), switchMap(() => this.portal.overview()), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: overview => { this.overview = overview; this.loading = false; this.error = false; },
      error: () => { this.loading = false; this.error = true; }
    });
    this.portal.sessions({ page: 1, pageSize: 5 }).pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: page => this.recent = page.items, error: () => this.error = true });
  }

  print(): void { window.print(); }
}
