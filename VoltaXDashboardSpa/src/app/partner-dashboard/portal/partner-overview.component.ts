import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, forkJoin, of, switchMap, timer } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';
import { formatNumber } from '@angular/common';
import { PartnerOverview, PartnerProfile, PartnerSession, PartnerStation, PartnerStatistics } from 'src/_models/portal';
import { LanguageService } from 'src/_services/language.service';
import { PartnerPortalService } from 'src/_services/portal/partner-portal.service';
import { statusTone } from 'src/app/shared/portal-ui/portal-ui.module';

/** Order in which connector states are listed (the API only returns the ones present). */
export const CONNECTOR_STATES = ['Available', 'Occupied', 'Reserved', 'Faulted', 'Unavailable'];

/** Partner home: brand, revenue (with the last 30 days), connector states, stations and the latest sessions, refreshed every minute. */
@Component({
  selector: 'app-partner-overview',
  templateUrl: './partner-overview.component.html'
})
export class PartnerOverviewComponent implements OnInit {
  private readonly portal = inject(PartnerPortalService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly language = inject(LanguageService);
  private readonly translate = inject(TranslateService);

  profile: PartnerProfile | null = null;
  overview: PartnerOverview | null = null;
  stations: PartnerStation[] = [];
  recent: PartnerSession[] = [];
  statistics: PartnerStatistics | null = null;
  /** ApexCharts options of the 30-day revenue chart. */
  trend: any = null;
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
        recent: this.portal.sessions({ page: 1, pageSize: 6 }),
        statistics: this.portal.statistics().pipe(catchError(() => of(null)))
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
      this.statistics = data.statistics;
      this.trend = this.chart();
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

  /** Revenue of the last 30 days, summed. */
  get revenue30(): number { return (this.statistics?.daily ?? []).reduce((sum, p) => sum + p.revenue, 0); }

  print(): void { window.print(); }

  private chart() {
    const points = this.statistics?.daily ?? [];
    if (!points.length) return null;
    const dark = document.body.classList.contains('dark');
    const locale = this.language.locale;
    const money = (v: number) => `${formatNumber(v, locale, '1.0-0')} ${this.translate.instant('common.currency.mad')}`;
    return {
      series: [{ name: this.translate.instant('partner.statistics.revenue'), data: points.map(p => Math.round(p.revenue * 100) / 100) }],
      chart: { type: 'area', height: 180, toolbar: { show: false }, sparkline: { enabled: false }, fontFamily: 'inherit', animations: { enabled: false },
        foreColor: dark ? '#A5B0C6' : '#737985' },
      colors: [dark ? '#7FA2EE' : '#1F3461'],
      stroke: { curve: 'smooth', width: 2 },
      fill: { type: 'gradient', gradient: { opacityFrom: 0.35, opacityTo: 0.02 } },
      dataLabels: { enabled: false },
      grid: { borderColor: dark ? 'rgba(255,255,255,.08)' : '#EDEDED', strokeDashArray: 4 },
      xaxis: { categories: points.map(p => new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'short' }).format(new Date(p.period + 'T00:00:00'))),
        tickAmount: 6, axisBorder: { show: false }, axisTicks: { show: false }, labels: { rotate: 0, hideOverlappingLabels: true } },
      yaxis: { labels: { formatter: money } },
      tooltip: { theme: dark ? 'dark' : 'light', y: { formatter: money } }
    };
  }
}
