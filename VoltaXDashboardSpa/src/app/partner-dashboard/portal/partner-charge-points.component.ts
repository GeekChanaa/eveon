import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { PartnerChargePoint, PartnerConnector, PartnerStation } from 'src/_models/portal';
import { PartnerPortalService } from 'src/_services/portal/partner-portal.service';
import { connectorTypeLabel, statusTone } from 'src/app/shared/portal-ui/portal-ui.module';
import { PricingChange } from './partner-pricing-editor.component';

/** The partner's charge points with their connectors' live state and prices, which the partner can change. */
@Component({
  selector: 'app-partner-charge-points',
  templateUrl: './partner-charge-points.component.html'
})
export class PartnerChargePointsComponent implements OnInit {
  private readonly portal = inject(PartnerPortalService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  stations: PartnerStation[] = [];
  chargePoints: PartnerChargePoint[] = [];
  stationId: number | null = null;
  loading = true;
  error = false;
  editing: number | null = null;
  saving = false;
  saveError: string | null = null;
  savedConnectors = new Set<number>();
  readonly tone = statusTone;
  readonly typeLabel = connectorTypeLabel;

  ngOnInit(): void {
    this.portal.stations().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ next: stations => this.stations = stations, error: () => {} });
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
      const station = Number(params.get('station'));
      this.stationId = Number.isInteger(station) && station > 0 ? station : null;
      this.load();
    });
  }

  load(): void {
    this.loading = true;
    this.portal.chargePoints(this.stationId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: points => { this.chargePoints = points; this.loading = false; this.error = false; },
      error: () => { this.loading = false; this.error = true; }
    });
  }

  selectStation(id: string): void {
    this.editing = null;
    this.router.navigate([], { relativeTo: this.route, queryParams: { station: id || null } });
  }

  edit(connector: PartnerConnector): void {
    this.editing = connector.id;
    this.saveError = null;
  }

  save(point: PartnerChargePoint, connector: PartnerConnector, change: PricingChange): void {
    const targets = change.applyToAll ? point.connectors : [connector];
    this.saving = true;
    this.saveError = null;
    forkJoin(targets.map(c => this.portal.updatePricing(c.id, change.pricing))).subscribe({
      next: updated => {
        point.connectors = point.connectors.map(c => updated.find(u => u.id === c.id) ?? c);
        this.savedConnectors = new Set(updated.map(u => u.id));
        this.saving = false;
        this.editing = null;
      },
      error: err => {
        this.saving = false;
        this.saveError = err?.status === 400 ? 'partner.pricing.invalid' : 'partner.pricing.failed';
      }
    });
  }

  print(): void { window.print(); }

  connectorLabel(c: PartnerConnector): string {
    return c.connectorId == null || c.connectorId === 1 ? `EVSE ${c.evseId}` : `EVSE ${c.evseId} / ${c.connectorId}`;
  }
}
