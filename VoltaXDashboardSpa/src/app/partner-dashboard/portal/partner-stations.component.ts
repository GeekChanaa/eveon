import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PartnerStation } from 'src/_models/portal';
import { PartnerPortalService } from 'src/_services/portal/partner-portal.service';
import { statusTone } from 'src/app/shared/portal-ui/portal-ui.module';

/** The partner's charging stations, as cards with their photo, access and equipment, each opening its page. */
@Component({
  selector: 'app-partner-stations',
  template: `
    <div class="page__inner"><main class="page__container">
      <app-partner-print-header [title]="'partner.stations.title' | translate"></app-partner-print-header>
      <header class="vx-heading">
        <div>
          <span class="vx-eyebrow">{{ 'partner.eyebrow' | translate }}</span>
          <h1>{{ 'partner.stations.title' | translate }}</h1>
          <p>{{ 'portal.partner.stationsSubtitle' | translate }}</p>
        </div>
        <div class="vx-actions no-print">
          <button type="button" class="vx-btn" (click)="print()">{{ 'common.actions.print' | translate }}</button>
        </div>
      </header>

      <div class="vx-alert vx-alert--error" *ngIf="error" role="alert">{{ 'common.state.error' | translate }}</div>
      <p class="vx-empty" *ngIf="loading">{{ 'common.state.loading' | translate }}</p>

      <div class="vx-station-cards" *ngIf="stations.length; else empty">
        <article class="vx-station-card" *ngFor="let st of stations" [attr.aria-labelledby]="'station-' + st.id">
          <div class="vx-station-card__photo">
            <img *ngIf="fileUrl(st.photoUrl) as photo; else noPhoto" [src]="photo" alt="">
            <ng-template #noPhoto><vx-icon name="charging-station"></vx-icon></ng-template>
          </div>
          <div class="vx-station-card__body">
            <div class="vx-panel__head" style="margin: 0">
              <h3 [id]="'station-' + st.id">{{ st.name }}</h3>
              <span class="vx-badge" [ngClass]="tone(st.status)">{{ 'partner.stationStatus.' + st.status | translate }}</span>
            </div>
            <p>{{ st.address }}, {{ st.city }}</p>
            <div class="vx-station-card__stats">
              <span>{{ 'partner.stations.chargePoints' | translate }} <strong class="vx-num">{{ st.chargePointCount }}</strong></span>
              <span>{{ 'partner.stations.connectors' | translate }} <strong class="vx-num">{{ st.connectorCount }}</strong></span>
              <span>{{ 'portal.station.access.' + st.access | translate }}</span>
            </div>
            <div class="vx-actions no-print">
              <a class="vx-btn vx-btn--small vx-btn--primary" [routerLink]="['/partner-dashboard/stations', st.id]">{{ 'portal.station.open' | translate }}</a>
              <a class="vx-btn vx-btn--small" [routerLink]="['/partner-dashboard/charge-points']" [queryParams]="{ station: st.id }">{{ 'partner.stations.pricesLink' | translate }}</a>
              <a class="vx-btn vx-btn--small" [routerLink]="['/partner-dashboard/sessions']" [queryParams]="{ station: st.id }">{{ 'partner.stations.sessionsLink' | translate }}</a>
              <a *ngIf="st.latitude && st.longitude" class="vx-btn vx-btn--small vx-btn--link" target="_blank" rel="noopener"
                [href]="'https://www.google.com/maps/search/?api=1&query=' + st.latitude + ',' + st.longitude">{{ 'partner.stations.map' | translate }}</a>
            </div>
          </div>
        </article>
      </div>
      <ng-template #empty>
        <section class="vx-panel" *ngIf="!loading && !error">
          <div class="vx-empty-state">
            <span class="vx-empty-state__icon"><vx-icon name="charging-station"></vx-icon></span>
            <strong>{{ 'partner.stations.empty' | translate }}</strong>
          </div>
        </section>
      </ng-template>
      <vx-print-footer [note]="'portal.partner.printNote' | translate"></vx-print-footer>
    </main></div>`
})
export class PartnerStationsComponent implements OnInit {
  private readonly portal = inject(PartnerPortalService);
  private readonly destroyRef = inject(DestroyRef);

  stations: PartnerStation[] = [];
  loading = true;
  error = false;
  readonly tone = statusTone;
  readonly fileUrl = PartnerPortalService.fileUrl;

  ngOnInit(): void {
    this.portal.stations().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: stations => { this.stations = stations; this.loading = false; },
      error: () => { this.loading = false; this.error = true; }
    });
  }

  print(): void { window.print(); }
}
