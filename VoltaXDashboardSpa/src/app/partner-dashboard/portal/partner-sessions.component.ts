import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { saveDownload } from 'src/_helpers/download';
import { PagedResult, PartnerChargePoint, PartnerProfile, PartnerSession, PartnerSessionFilter, PartnerStation } from 'src/_models/portal';
import { PartnerPortalService } from 'src/_services/portal/partner-portal.service';

const EMPTY: PartnerSessionFilter = { from: null, to: null, stationId: null, chargePointId: null, status: '', page: 1, pageSize: 25 };

/** Sessions on the partner's stations (no driver data): filters, paging, CSV / PDF statement and print. */
@Component({
  selector: 'app-partner-sessions',
  templateUrl: './partner-sessions.component.html'
})
export class PartnerSessionsComponent implements OnInit {
  private readonly portal = inject(PartnerPortalService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  filter: PartnerSessionFilter = { ...EMPTY };
  result: PagedResult<PartnerSession> | null = null;
  stations: PartnerStation[] = [];
  chargePoints: PartnerChargePoint[] = [];
  profile: PartnerProfile | null = null;
  loading = false;
  error = false;
  exporting: 'csv' | 'pdf' | null = null;
  readonly fileUrl = PartnerPortalService.fileUrl;

  ngOnInit(): void {
    this.portal.profile$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(profile => this.profile = profile);
    this.portal.stations().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ next: s => this.stations = s, error: () => {} });
    this.portal.chargePoints().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ next: p => this.chargePoints = p, error: () => {} });
    const params = this.route.snapshot.queryParamMap;
    this.filter.stationId = this.id(params.get('station'));
    this.filter.chargePointId = this.id(params.get('chargePoint'));
    this.load();
  }

  /** Charge points of the selected station (all of them when no station is selected). */
  get visibleChargePoints(): PartnerChargePoint[] {
    return this.filter.stationId ? this.chargePoints.filter(cp => cp.stationId === Number(this.filter.stationId)) : this.chargePoints;
  }

  load(page = 1): void {
    this.filter.page = page;
    this.loading = true;
    this.portal.sessions(this.filter).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => { this.result = result; this.loading = false; this.error = false; },
      error: () => { this.loading = false; this.error = true; }
    });
  }

  stationChanged(): void {
    if (this.filter.chargePointId && !this.visibleChargePoints.some(cp => cp.id === Number(this.filter.chargePointId)))
      this.filter.chargePointId = null;
  }

  reset(): void {
    this.filter = { ...EMPTY };
    this.load();
  }

  get totals(): { minutes: number; kwh: number; revenue: number } {
    const items = this.result?.items ?? [];
    return {
      minutes: items.reduce((sum, s) => sum + s.chargedMinutes, 0),
      kwh: items.reduce((sum, s) => sum + s.chargedKwhs, 0),
      revenue: items.reduce((sum, s) => sum + s.total, 0)
    };
  }

  get period(): string | null {
    return this.filter.from || this.filter.to ? `${this.filter.from || '…'} → ${this.filter.to || '…'}` : null;
  }

  export(format: 'csv' | 'pdf'): void {
    this.exporting = format;
    this.portal.statement(this.filter, format).subscribe({
      next: response => { saveDownload(response, `eveon-partner-statement.${format}`); this.exporting = null; },
      error: () => { this.exporting = null; this.error = true; }
    });
  }

  print(): void { window.print(); }

  private id(value: string | null): number | null {
    const n = Number(value);
    return Number.isInteger(n) && n > 0 ? n : null;
  }
}
