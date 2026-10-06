import { Component, OnDestroy, OnInit } from '@angular/core';
import { Observable, Subscription, catchError, forkJoin, of } from 'rxjs';
import { OrderService } from 'src/_services/order.service';
import { TransactionService } from 'src/_services/transaction.service';
import { StatisticsService } from 'src/_services/statistics.service';

interface Metric {
  key: string;
  label: string;
  unit: string;
  icon: string;
  total: number | null;
  today: number | null;
  loading: boolean;
  error: boolean;
  chart: any;
}

@Component({
  selector: 'app-tabs-statistics',
  templateUrl: './tabs-statistics.component.html',
  styleUrls: ['./tabs-statistics.component.css']
})
export class TabsStatisticsComponent implements OnInit, OnDestroy {
  metrics: Metric[] = [
    { key: 'energy', label: 'Energy delivered', unit: 'kWh', icon: 'icon-energy', total: null, today: null, loading: true, error: false, chart: null },
    { key: 'orders', label: 'Recharge orders', unit: 'orders', icon: 'icon-basket', total: null, today: null, loading: true, error: false, chart: null },
    { key: 'recharge', label: 'Recharge amount', unit: 'MAD', icon: 'icon-ticket', total: null, today: null, loading: true, error: false, chart: null },
    { key: 'revenue', label: 'Revenue', unit: 'MAD', icon: 'icon-bar-chart', total: null, today: null, loading: true, error: false, chart: null }
  ];
  selected = this.metrics[0];
  private requests = new Subscription();
  constructor(private transactions: TransactionService, private orders: OrderService, private statistics: StatisticsService) {}
  ngOnInit() { this.metrics.forEach(metric => this.load(metric)); }
  ngOnDestroy() { this.requests.unsubscribe(); }

  select(metric: Metric) { this.selected = metric; }
  moveTab(event: KeyboardEvent, index: number) {
    let next = index;
    if (event.key === 'ArrowRight') next = (index + 1) % this.metrics.length;
    else if (event.key === 'ArrowLeft') next = (index + this.metrics.length - 1) % this.metrics.length;
    else if (event.key === 'Home') next = 0;
    else if (event.key === 'End') next = this.metrics.length - 1;
    else return;
    event.preventDefault();
    this.select(this.metrics[next]);
    const buttons = (event.currentTarget as HTMLElement).parentElement?.querySelectorAll<HTMLButtonElement>('[role="tab"]');
    buttons?.[next].focus();
  }

  load(metric: Metric) {
    metric.loading = true;
    metric.error = false;
    const sources: Record<string, Observable<any>[]> = {
      energy: [this.transactions.getTotalEnergyConsumed(), this.transactions.getTotalEnergyConsumedToday(), this.transactions.getDailyEnergyConsumedLast30Days()],
      orders: [this.orders.count(), this.orders.countOrdersToday(), this.orders.countOrdersByDay()],
      recharge: [this.orders.countRechargeAmount(), this.orders.countRechargeAmountToday(), this.orders.countRechargeAmountByDay()],
      revenue: [this.statistics.getTotalRevenue(), this.statistics.getTotalRevenueToday(), this.statistics.getDailyRevenueLast30Days()]
    };
    this.requests.add(forkJoin(sources[metric.key].map(source => source.pipe(catchError(() => of(null))))).subscribe(([total, today, daily]) => {
      metric.total = typeof total === 'number' && Number.isFinite(total) ? total : null;
      metric.today = typeof today === 'number' && Number.isFinite(today) ? today : null;
      metric.error = metric.total === null || metric.today === null || daily === null;
      metric.chart = daily === null ? null : this.chartOptions(metric, daily);
      metric.loading = false;
    }));
  }

  private chartOptions(metric: Metric, daily: Record<string, number>) {
    const entries = Object.entries(daily).filter(([, value]) => typeof value === 'number' && Number.isFinite(value));
    // Dictionary keys are API dates. Preserve numeric indices if an endpoint returns an array.
    if (!Array.isArray(daily)) entries.sort(([a], [b]) => a.localeCompare(b));
    const categories = entries.map(([key]) => {
      if (/^\d+$/.test(key)) return 'Day ' + String(Number(key) + 1);
      const date = new Date(key);
      return Number.isNaN(date.getTime()) ? key : date.toLocaleDateString(undefined, { day: 'numeric', month: 'short' });
    });
    return {
      series: [{ name: metric.label + ' (' + metric.unit + ')', data: entries.map(([, value]) => Number(value.toFixed(2))) }],
      chart: { type: 'area', height: 280, toolbar: { show: false }, fontFamily: 'Inter, sans-serif', foreColor: '#858990', parentHeightOffset: 0, animations: { enabled: false } },
      colors: ['#D4A321'],
      stroke: { curve: 'straight', width: 2.5 },
      fill: { type: 'gradient', gradient: { opacityFrom: 0.22, opacityTo: 0.01 } },
      dataLabels: { enabled: false },
      xaxis: { categories, tickAmount: 6, axisBorder: { show: false }, axisTicks: { show: false }, labels: { hideOverlappingLabels: true, rotate: 0, style: { colors: '#858990', fontSize: '11px' } }, tooltip: { enabled: false } },
      yaxis: { labels: { formatter: (value: number) => value.toLocaleString(undefined, { maximumFractionDigits: metric.key === 'orders' ? 0 : 1 }), style: { colors: '#858990', fontSize: '11px' } } },
      grid: { borderColor: 'rgba(128,128,128,.16)', strokeDashArray: 4, padding: { left: 8, right: 12 } },
      tooltip: { theme: document.body.classList.contains('dark') ? 'dark' : 'light', y: { formatter: (value: number) => value.toLocaleString(undefined, { maximumFractionDigits: 2 }) + ' ' + metric.unit } },
      empty: entries.length === 0
    };
  }
}
