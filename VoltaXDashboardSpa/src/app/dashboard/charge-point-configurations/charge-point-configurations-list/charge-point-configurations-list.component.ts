import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription, interval, switchMap } from 'rxjs';
import { PageState } from 'src/_models/_enums/page-state.enum';
import { ChargePointConfigurationStatus } from 'src/_models/charge-point-configuration';
import { OcppDeviceModelService } from 'src/_services/ocpp-services/ocpp-device-model.service';

type Filter = 'all' | 'setup' | 'configured' | 'online' | 'offline';

/** Every charge point with its connection and configuration state; entry point to setup and device-model editing. */
@Component({
  selector: 'app-charge-point-configurations-list',
  templateUrl: './charge-point-configurations-list.component.html',
  styleUrls: ['./charge-point-configurations-list.component.sass']
})
export class ChargePointConfigurationsListComponent implements OnInit, OnDestroy {

  PageState = PageState;
  state: PageState = PageState.Loading;

  all: ChargePointConfigurationStatus[] = [];
  // Bound to *ngFor: only replaced when the list, the search or the filter changes.
  visible: ChargePointConfigurationStatus[] = [];
  search = '';
  filter: Filter = 'all';

  readonly filters: { key: Filter; label: string }[] = [
    { key: 'all', label: 'All' },
    { key: 'setup', label: 'Needs setup' },
    { key: 'configured', label: 'Configured' },
    { key: 'online', label: 'Online' },
    { key: 'offline', label: 'Offline' }
  ];

  stats = { total: 0, online: 0, setup: 0, configured: 0 };

  private poll?: Subscription;

  constructor(private _deviceModel: OcppDeviceModelService) { }

  ngOnInit() {
    this._deviceModel.overview().subscribe({
      next: list => { this.setList(list); this.state = PageState.Success; },
      error: () => this.state = PageState.Error
    });
    // Connection state changes on its own; keep it fresh without reloading the page.
    this.poll = interval(15000).pipe(switchMap(() => this._deviceModel.overview()))
      .subscribe({ next: list => this.setList(list), error: () => { } });
  }

  ngOnDestroy() {
    this.poll?.unsubscribe();
  }

  setFilter(filter: Filter) {
    this.filter = filter;
    this.applyFilter();
  }

  applyFilter() {
    const term = this.search.trim().toLowerCase();
    this.visible = this.all.filter(cp => {
      if (this.filter === 'setup' && cp.isConfigured) return false;
      if (this.filter === 'configured' && !cp.isConfigured) return false;
      if (this.filter === 'online' && !cp.isOnline) return false;
      if (this.filter === 'offline' && cp.isOnline) return false;
      return !term || [cp.chargePointId, cp.stationName, cp.vendorName, cp.modelName, cp.serialNumber]
        .some(text => (text ?? '').toLowerCase().includes(term));
    });
  }

  count(filter: Filter): number {
    switch (filter) {
      case 'setup': return this.stats.setup;
      case 'configured': return this.stats.configured;
      case 'online': return this.stats.online;
      case 'offline': return this.stats.total - this.stats.online;
      default: return this.stats.total;
    }
  }

  badge(cp: ChargePointConfigurationStatus): { label: string; tone: string } {
    if (cp.configurationStatus === 'AwaitingReboot' && cp.isConfigured) return { label: 'Awaiting reboot', tone: 'warn' };
    if (cp.isConfigured) return { label: 'Configured', tone: 'ok' };
    if (cp.configurationMethod === 'Skipped') return { label: 'Accepted as-is', tone: 'muted' };
    if (cp.configurationMethod === 'Legacy') return { label: 'Not configured', tone: 'muted' };
    return { label: 'Needs setup', tone: 'pending' };
  }

  trackCp = (_: number, cp: ChargePointConfigurationStatus) => cp.chargePointId;

  private setList(list: ChargePointConfigurationStatus[]) {
    this.all = list;
    this.stats = {
      total: list.length,
      online: list.filter(cp => cp.isOnline).length,
      setup: list.filter(cp => !cp.isConfigured).length,
      configured: list.filter(cp => cp.isConfigured).length
    };
    this.applyFilter();
  }
}
