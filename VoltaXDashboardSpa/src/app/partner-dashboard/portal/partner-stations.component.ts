import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PartnerStation } from 'src/_models/portal';
import { PartnerPortalService } from 'src/_services/portal/partner-portal.service';
import { statusTone } from 'src/app/shared/portal-ui/portal-ui.module';

/** The partner's charging stations. */
@Component({
  selector: 'app-partner-stations',
  template: `
    <div class="page__inner"><main class="page__container">
      <vx-print-header [title]="'partner.stations.title' | translate"></vx-print-header>
      <header class="vx-heading">
        <div>
          <span class="vx-eyebrow">{{ 'partner.eyebrow' | translate }}</span>
          <h1>{{ 'partner.stations.title' | translate }}</h1>
          <p>{{ 'partner.stations.subtitle' | translate }}</p>
        </div>
        <div class="vx-actions no-print">
          <button type="button" class="vx-btn" (click)="print()">{{ 'common.actions.print' | translate }}</button>
        </div>
      </header>

      <div class="vx-alert vx-alert--error" *ngIf="error" role="alert">{{ 'common.state.error' | translate }}</div>

      <section class="vx-panel">
        <p class="vx-empty" *ngIf="loading">{{ 'common.state.loading' | translate }}</p>
        <div class="vx-table-wrap" *ngIf="stations.length; else empty">
          <table class="vx-table">
            <thead><tr>
              <th>{{ 'partner.fields.station' | translate }}</th>
              <th>{{ 'partner.fields.address' | translate }}</th>
              <th>{{ 'common.fields.status' | translate }}</th>
              <th class="num">{{ 'partner.stations.chargePoints' | translate }}</th>
              <th class="num">{{ 'partner.stations.connectors' | translate }}</th>
              <th class="no-print"><span class="vx-sr-only">{{ 'common.actions.details' | translate }}</span></th>
            </tr></thead>
            <tbody>
              <tr *ngFor="let st of stations">
                <td><strong>{{ st.name }}</strong></td>
                <td>{{ st.address }}<div class="muted">{{ st.city }}</div></td>
                <td><span class="vx-badge" [ngClass]="tone(st.status)">{{ 'partner.stationStatus.' + st.status | translate }}</span></td>
                <td class="num">{{ st.chargePointCount }}</td>
                <td class="num">{{ st.connectorCount }}</td>
                <td class="no-print num">
                  <span class="vx-actions">
                    <a class="vx-btn vx-btn--small" [routerLink]="['/partner-dashboard/charge-points']" [queryParams]="{ station: st.id }">{{ 'partner.stations.pricesLink' | translate }}</a>
                    <a class="vx-btn vx-btn--small" [routerLink]="['/partner-dashboard/sessions']" [queryParams]="{ station: st.id }">{{ 'partner.stations.sessionsLink' | translate }}</a>
                    <a *ngIf="st.latitude && st.longitude" class="vx-btn vx-btn--small vx-btn--link" target="_blank" rel="noopener"
                      [href]="'https://www.google.com/maps/search/?api=1&query=' + st.latitude + ',' + st.longitude">{{ 'partner.stations.map' | translate }}</a>
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <ng-template #empty><p class="vx-empty" *ngIf="!loading">{{ 'partner.stations.empty' | translate }}</p></ng-template>
      </section>
    </main></div>`
})
export class PartnerStationsComponent implements OnInit {
  private readonly portal = inject(PartnerPortalService);
  private readonly destroyRef = inject(DestroyRef);

  stations: PartnerStation[] = [];
  loading = true;
  error = false;
  readonly tone = statusTone;

  ngOnInit(): void {
    this.portal.stations().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: stations => { this.stations = stations; this.loading = false; },
      error: () => { this.loading = false; this.error = true; }
    });
  }

  print(): void { window.print(); }
}
