import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { formatNumber } from '@angular/common';
import { TranslateService } from '@ngx-translate/core';
import { PartnerStatisticPoint, PartnerStatistics } from 'src/_models/portal';
import { LanguageService } from 'src/_services/language.service';
import { PartnerPortalService } from 'src/_services/portal/partner-portal.service';

export type StatisticMetric = 'revenue' | 'energy' | 'sessions';

/** Revenue, energy and sessions on the partner's stations: last 30 days and last 12 months. */
@Component({
  selector: 'app-partner-statistics',
  templateUrl: './partner-statistics.component.html'
})
export class PartnerStatisticsComponent implements OnInit {
  private readonly portal = inject(PartnerPortalService);
  private readonly language = inject(LanguageService);
  private readonly translate = inject(TranslateService);
  private readonly destroyRef = inject(DestroyRef);

  stats: PartnerStatistics | null = null;
  metric: StatisticMetric = 'revenue';
  daily: any = null;
  monthly: any = null;
  loading = true;
  error = false;
  readonly metrics: StatisticMetric[] = ['revenue', 'energy', 'sessions'];

  ngOnInit(): void {
    this.portal.statistics().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: stats => { this.stats = stats; this.loading = false; this.build(); },
      error: () => { this.loading = false; this.error = true; }
    });
    this.language.changes.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.build());
  }

  select(metric: StatisticMetric): void {
    this.metric = metric;
    this.build();
  }

  total(points: PartnerStatisticPoint[] | undefined, metric: StatisticMetric): number {
    return (points ?? []).reduce((sum, p) => sum + p[metric], 0);
  }

  /** "2026-10" → "Oct 2026", "2026-10-23" → "23 Oct", in the current language. */
  label(period: string): string {
    const [y, m, d] = period.split('-').map(Number);
    const date = new Date(y, m - 1, d || 1);
    return new Intl.DateTimeFormat(this.language.locale, d ? { day: 'numeric', month: 'short' } : { month: 'short', year: 'numeric' }).format(date);
  }

  print(): void { window.print(); }

  private build(): void {
    if (!this.stats) return;
    this.daily = this.chart(this.stats.daily);
    this.monthly = this.chart(this.stats.monthly);
  }

  private format(value: number): string {
    const locale = this.language.locale;
    if (this.metric === 'revenue') return `${formatNumber(value, locale, '1.0-2')} ${this.translate.instant('common.currency.mad')}`;
    if (this.metric === 'energy') return `${formatNumber(value, locale, '1.0-1')} ${this.translate.instant('common.units.kwh')}`;
    return formatNumber(value, locale, '1.0-0');
  }

  private chart(points: PartnerStatisticPoint[]) {
    const dark = document.body.classList.contains('dark');
    return {
      series: [{ name: this.translate.instant('partner.statistics.' + this.metric), data: points.map(p => Math.round(p[this.metric] * 100) / 100) }],
      chart: { type: 'bar', height: 260, toolbar: { show: false }, fontFamily: 'inherit', foreColor: dark ? '#A5B0C6' : '#737985', animations: { enabled: false } },
      colors: [dark ? '#7FA2EE' : '#1F3461'],
      plotOptions: { bar: { borderRadius: 4, columnWidth: '62%' } },
      dataLabels: { enabled: false },
      grid: { borderColor: dark ? 'rgba(255,255,255,.08)' : '#EDEDED', strokeDashArray: 4 },
      xaxis: { categories: points.map(p => this.label(p.period)), axisBorder: { show: false }, axisTicks: { show: false }, labels: { hideOverlappingLabels: true, rotate: 0 } },
      yaxis: { labels: { formatter: (v: number) => this.format(v) } },
      tooltip: { theme: dark ? 'dark' : 'light', y: { formatter: (v: number) => this.format(v) } }
    };
  }
}
