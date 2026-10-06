import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription, interval } from 'rxjs';
import { PageState } from 'src/_models/_enums/page-state.enum';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import {
  apiErrorMessage, LoadBalancingStation, RebalanceSummary, SmartChargingService, StationLoadAllocation, StationLoadLimit
} from 'src/_services/smart-charging.service';

const REFRESH_MS = 15000;

@Component({
  selector: 'app-station-load-balance',
  templateUrl: './station-load-balance.component.html',
  styleUrls: ['../smart-charging-shared.css', './station-load-balance.component.css']
})
export class StationLoadBalanceComponent implements OnInit, OnDestroy {

  PageState = PageState;
  state: PageState = PageState.Loading;

  stations: LoadBalancingStation[] = [];
  stationFilter = '';
  selected: LoadBalancingStation | null = null;

  limit: StationLoadLimit | null = null;
  saving = false;
  rebalancing = false;
  live: RebalanceSummary | null = null;
  history: StationLoadAllocation[] = [];

  private refresh?: Subscription;

  constructor(
    private _smartCharging: SmartChargingService,
    private _modal: ActionModalService
  ) { }

  ngOnInit() {
    this.loadStations(true);
    this.refresh = interval(REFRESH_MS).subscribe(() => {
      if (this.selected) this.loadLive();
    });
  }

  ngOnDestroy() {
    this.refresh?.unsubscribe();
  }

  get filteredStations(): LoadBalancingStation[] {
    const filter = this.stationFilter.trim().toLowerCase();
    return filter ? this.stations.filter(s => (s.name + ' ' + (s.city ?? '')).toLowerCase().includes(filter)) : this.stations;
  }

  /** Station limit (A per phase) the allocator will use, as computed by the API (lower of the two limits, minus the margin). */
  get previewLimitA(): number {
    const l = this.limit;
    if (!l) return 0;
    let limit: number | null = l.maxCurrentA && l.maxCurrentA > 0 ? Number(l.maxCurrentA) : null;
    if (l.maxPowerKW && l.maxPowerKW > 0 && l.voltage > 0 && l.phases > 0) {
      const fromPower = Number(l.maxPowerKW) * 1000 / (Number(l.voltage) * Number(l.phases));
      limit = limit == null ? fromPower : Math.min(limit, fromPower);
    }
    if (limit == null) return 0;
    const margin = Math.min(Math.max(Number(l.safetyMarginPercent) || 0, 0), 100);
    return Math.floor(limit * (1 - margin / 100) * 10 + 1e-6) / 10;
  }

  get previewMaxSessions(): number | null {
    const min = Number(this.limit?.minPerSessionA) || 0;
    return min > 0 ? Math.floor(this.previewLimitA / min) : null;
  }

  loadStations(first = false) {
    this._smartCharging.getLoadBalancingStations().subscribe({
      next: stations => {
        this.stations = stations;
        this.state = PageState.Success;
        if (this.selected) this.selected = stations.find(s => s.id === this.selected!.id) ?? null;
        else if (first && stations.length > 0) this.selectStation(stations[0]);
      },
      error: () => this.state = PageState.Error
    });
  }

  selectStation(station: LoadBalancingStation) {
    this.selected = station;
    this.limit = null;
    this.live = null;
    this.history = [];
    this._smartCharging.getStationLoadLimit(station.id).subscribe({
      next: limit => this.limit = { ...limit, strategy: limit.strategy ?? 'EqualShare' },
      error: err => this.error(err)
    });
    this.loadLive();
    this.loadHistory();
  }

  loadLive() {
    if (!this.selected) return;
    const stationId = this.selected.id;
    this._smartCharging.getStationAllocations(stationId).subscribe({
      next: live => { if (this.selected?.id === stationId) this.live = live; },
      error: () => this.live = null
    });
  }

  loadHistory() {
    if (!this.selected) return;
    this._smartCharging.getStationAllocationHistory(this.selected.id, 50).subscribe({
      next: history => this.history = history,
      error: () => this.history = []
    });
  }

  save() {
    if (!this.selected || !this.limit) return;
    const l = this.limit;
    const body: StationLoadLimit = {
      chargingStationID: this.selected.id,
      enabled: !!l.enabled,
      maxCurrentA: l.maxCurrentA ? Number(l.maxCurrentA) : null,
      maxPowerKW: l.maxPowerKW ? Number(l.maxPowerKW) : null,
      phases: Number(l.phases),
      voltage: Number(l.voltage),
      minPerSessionA: Number(l.minPerSessionA),
      strategy: l.strategy,
      safetyMarginPercent: Number(l.safetyMarginPercent) || 0
    };
    this.saving = true;
    this._smartCharging.saveStationLoadLimit(this.selected.id, body).subscribe({
      next: saved => {
        this.saving = false;
        this.limit = saved;
        this._modal.popup(ActionModalStatusEnum.Success, "Success !", "Load limit saved; the station is rebalanced within a few seconds.", 4000);
        this.loadStations();
        setTimeout(() => { this.loadLive(); this.loadHistory(); }, 8000);
      },
      error: err => {
        this.saving = false;
        this.error(err);
      }
    });
  }

  rebalance() {
    if (!this.selected) return;
    this.rebalancing = true;
    this._smartCharging.rebalanceStation(this.selected.id).subscribe({
      next: summary => {
        this.rebalancing = false;
        this.live = summary;
        const status = summary.failed > 0 ? ActionModalStatusEnum.Warning : ActionModalStatusEnum.Success;
        const message = !summary.enabled
          ? "Load balancing is disabled for this station."
          : `${summary.sent} profile(s) accepted, ${summary.unchanged} unchanged, ${summary.failed} failed.`;
        this._modal.popup(status, status === ActionModalStatusEnum.Success ? "Success !" : "Warning !", message, 5000);
        this.loadHistory();
      },
      error: err => {
        this.rebalancing = false;
        this.error(err);
      }
    });
  }

  statusClass(status: string | null | undefined): string {
    switch (status) {
      case 'Accepted': return 'sc-badge sc-badge--ok';
      case 'Rejected': case 'CallError': case 'Invalid': case 'Failed': return 'sc-badge sc-badge--error';
      case 'Timeout': case 'NotConnected': case 'Pending': return 'sc-badge sc-badge--warn';
      default: return 'sc-badge';
    }
  }

  private error(err: unknown) {
    this._modal.popup(ActionModalStatusEnum.Error, "Error !", apiErrorMessage(err), 5000);
  }
}
