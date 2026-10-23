import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, forkJoin, of, switchMap, timer } from 'rxjs';
import { PartnerOverview, PartnerProfile, PartnerSession, PartnerStation } from 'src/_models/portal';
import { PartnerPortalService } from 'src/_services/portal/partner-portal.service';
import { statusTone } from 'src/app/shared/portal-ui/portal-ui.module';

/** Order in which connector states are listed (the API only returns the ones present). */
export const CONNECTOR_STATES = ['Available', 'Occupied', 'Reserved', 'Faulted', 'Unavailable'];

/** Partner home: brand, revenue, connector states, stations and the latest sessions, refreshed every minute. */
@Component({
  selector: 'app-partner-overview',
  templateUrl: './partner-overview.component.html'
})
export class PartnerOverviewComponent implements OnInit {
  private readonly portal = inject(PartnerPortalService);
  private readonly destroyRef = inject(DestroyRef);

  profile: PartnerProfile | null = null;
  overview: PartnerOverview | null = null;
  stations: PartnerStation[] = [];
  recent: PartnerSession[] = [];
  loading = true;
  error = false;
  refreshedAt: Date | null = null;
  readonly tone = statusTone;
  readonly fileUrl = PartnerPortalService.fileUrl;

  ngOnInit(): void {
    timer(0, 60_000).pipe(
      switchMap(() => forkJoin({
        profile: this.portal.profile(),
        overview: this.portal.overview(),
        stations: this.portal.stations(),
        recent: this.portal.sessions({ page: 1, pageSize: 6 })
      }).pipe(catchError(() => of(null)))),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(data => {
      this.loading = false;
      this.error = data === null;
      if (!data) return;
      this.profile = data.profile;
      this.overview = data.overview;
      this.stations = data.stations;
      this.recent = data.recent.items;
      this.refreshedAt = new Date();
    });
  }

  /** Connector states with their count, in a stable order, including states the API did not return. */
  get connectorStates(): { state: string; count: number }[] {
    const counts = this.overview?.connectorsByStatus ?? {};
    const extra = Object.keys(counts).filter(state => !CONNECTOR_STATES.includes(state));
    return [...CONNECTOR_STATES, ...extra].map(state => ({ state, count: counts[state] ?? 0 }));
  }

  share(count: number): number {
    const total = this.overview?.connectors ?? 0;
    return total ? Math.round(count * 100 / total) : 0;
  }

  print(): void { window.print(); }
}
