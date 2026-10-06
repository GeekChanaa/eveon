import { Component, Input, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import * as signalR from '@microsoft/signalr';
import { environment } from 'src/environments/environment';
import { TokenStorageService } from 'src/_services/token-storage.service';
import { ChargerDeviceDataService, ChargerEvent, ChargerEventFilters, VariableMonitor } from 'src/_services/charger-device-data.service';

/** Hub group of the global alarm feed (ChargerAlarmNotifier.AlarmGroup on the API). */
const ALARM_GROUP = '#charger-events';

const SEVERITY_LABELS = ['Danger', 'Hardware failure', 'System failure', 'Critical', 'Error', 'Alert', 'Warning', 'Notice', 'Informational', 'Debug'];

/**
 * OCPP 2.0.1 charger events (NotifyEvent), newest first, with live alarms pushed over the charger hub.
 * With [chargePointId] it lists one charger (and its monitors); without, every charger.
 */
@Component({
  selector: 'app-charger-events',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './charger-events.component.html',
  styleUrls: ['./charger-events.component.sass']
})
export class ChargerEventsComponent implements OnInit, OnDestroy {
  @Input() chargePointId = '';
  readonly pageSize = 25;
  filters: ChargerEventFilters = { alarmsOnly: false, maxSeverity: null, from: '', to: '', search: '', component: '', variable: '' };
  items: ChargerEvent[] = [];
  monitors: VariableMonitor[] = [];
  showMonitors = false;
  page = 1;
  totalCount = 0;
  loading = false;
  error = '';
  live = false;
  liveCount = 0;
  private hub?: signalR.HubConnection;

  constructor(private data: ChargerDeviceDataService, private tokens: TokenStorageService) {}

  ngOnInit(): void {
    this.load();
    this.connect();
  }

  ngOnDestroy(): void {
    this.hub?.stop().catch(() => undefined);
  }

  get totalPages(): number { return Math.max(1, Math.ceil(this.totalCount / this.pageSize)); }

  severityLabel(severity?: number | null): string {
    return severity === null || severity === undefined ? '-' : `${severity} ${SEVERITY_LABELS[severity] ?? ''}`.trim();
  }

  isAlarm(e: ChargerEvent): boolean {
    return e.cleared !== true && ((e.severity ?? 99) <= 3 || /Problem|Tripped|Fault|Overheat|Fallback|Overcurrent|Overvoltage|Undervoltage/i.test(e.variableName)
      && e.actualValue !== 'false' && e.actualValue !== '0');
  }

  search(): void { this.page = 1; this.load(); }

  reset(): void {
    this.filters = { alarmsOnly: false, maxSeverity: null, from: '', to: '', search: '', component: '', variable: '' };
    this.search();
  }

  goTo(page: number): void {
    if (page < 1 || page > this.totalPages || page === this.page) return;
    this.page = page;
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = '';
    this.liveCount = 0;
    const filters: ChargerEventFilters = { ...this.filters, chargePointId: this.chargePointId || undefined };
    if (filters.from) filters.from = new Date(filters.from + 'T00:00:00').toISOString();
    if (filters.to) filters.to = new Date(filters.to + 'T23:59:59').toISOString();
    this.data.events(this.page, this.pageSize, filters).subscribe({
      next: result => { this.items = result.items; this.totalCount = result.totalCount; this.loading = false; },
      error: () => { this.items = []; this.totalCount = 0; this.loading = false; this.error = 'Could not load the charger events.'; }
    });
  }

  toggleMonitors(): void {
    this.showMonitors = !this.showMonitors;
    if (!this.showMonitors || !this.chargePointId) return;
    this.data.monitors(this.chargePointId).subscribe({
      next: monitors => this.monitors = monitors,
      error: () => { this.monitors = []; this.error = 'Could not load the monitors.'; }
    });
  }

  private connect(): void {
    this.hub = new signalR.HubConnectionBuilder()
      .withUrl(environment.apiUrl + '/chargerHub', { accessTokenFactory: () => this.tokens.accessToken || '' })
      .withAutomaticReconnect()
      .build();
    this.hub.on('ChargerEvent', (event: ChargerEvent) => this.onLiveEvent(event));
    const join = () => this.hub!.invoke('JoinChargerGroup', this.chargePointId || ALARM_GROUP).then(() => this.live = true);
    this.hub.onreconnected(() => join().catch(() => this.live = false));
    this.hub.onclose(() => this.live = false);
    this.hub.start().then(join).catch(() => this.live = false);
  }

  private onLiveEvent(event: ChargerEvent): void {
    if (this.chargePointId && event.chargePointID !== this.chargePointId) return;
    // Pushed events are alarms; show them at the top of the first page, unfiltered views only.
    if (this.page !== 1 || this.filters.search || this.filters.component || this.filters.variable || this.filters.from || this.filters.to) {
      this.liveCount++;
      return;
    }
    if (this.items.some(e => e.id === event.id)) return;
    this.items = [event, ...this.items].slice(0, this.pageSize);
    this.totalCount++;
  }
}
