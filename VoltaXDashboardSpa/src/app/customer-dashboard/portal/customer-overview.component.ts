import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, catchError, forkJoin, interval, merge, of, startWith, switchMap } from 'rxjs';
import { MyOverview, MySession } from 'src/_models/portal';
import { CustomerPortalService } from 'src/_services/portal/customer-portal.service';
import { LiveSessionUpdate } from 'src/_services/portal/live-session.service';
import { statusTone } from 'src/app/shared/portal-ui/portal-ui.module';

/**
 * Customer home: wallet, this month's charging, the running session live (with its stop button) and the latest
 * sessions. The page refreshes every 30 seconds and as soon as a session ends.
 */
@Component({
  selector: 'app-customer-overview',
  templateUrl: './customer-overview.component.html'
})
export class CustomerOverviewComponent implements OnInit {
  private readonly portal = inject(CustomerPortalService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly refresh$ = new Subject<void>();

  overview: MyOverview | null = null;
  recent: MySession[] = [];
  loading = true;
  error = false;
  /** The session that just ended under the user's eyes (summary shown until dismissed). */
  finished: LiveSessionUpdate | null = null;
  readonly tone = statusTone;

  ngOnInit(): void {
    merge(interval(30_000), this.refresh$).pipe(
      startWith(0),
      switchMap(() => forkJoin({ overview: this.portal.overview(), recent: this.portal.sessions({ page: 1, pageSize: 5 }) })
        .pipe(catchError(() => of(null)))),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(data => {
      this.loading = false;
      this.error = data === null;
      if (!data) return;
      this.overview = data.overview;
      this.recent = data.recent.items;
    });
  }

  /** The live widget saw the session end: show what it cost and reload the balance and history. */
  onEnded(update: LiveSessionUpdate): void {
    this.finished = update;
    this.refresh$.next();
  }

  retry(): void {
    this.loading = true;
    this.refresh$.next();
  }

  print(): void { window.print(); }
}
