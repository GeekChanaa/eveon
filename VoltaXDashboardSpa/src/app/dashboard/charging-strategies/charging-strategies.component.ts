import { Component, OnInit } from '@angular/core';
import { PageState } from 'src/_models/_enums/page-state.enum';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import {
  apiErrorMessage, ChargingProfileKind, ChargingProfilePurpose, ChargingProfileRecurrency, ChargingRateUnit, ChargingStrategy,
  ChargingStrategyInput, ChargingStrategyPeriod, formatOffset, SmartChargePoint, SmartChargingService, StrategyApplyResult
} from 'src/_services/smart-charging.service';

interface StrategyPeriodRow {
  day: number;
  time: string;
  offsetMinutes: number;
  limit: number;
  numberPhases: number | null;
}

interface StrategyForm {
  id: number | null;
  name: string;
  description: string;
  purpose: ChargingProfilePurpose;
  kind: ChargingProfileKind;
  recurrencyKind: ChargingProfileRecurrency;
  chargingRateUnit: ChargingRateUnit;
  stackLevel: number;
  periods: StrategyPeriodRow[];
}

interface TimelineSegment {
  widthPercent: number;
  opacity: number;
  title: string;
}

const DAY = 86400;
const WEEK = 7 * DAY;

@Component({
  selector: 'app-charging-strategies',
  templateUrl: './charging-strategies.component.html',
  styleUrls: ['../smart-charging-shared.css', './charging-strategies.component.css']
})
export class ChargingStrategiesComponent implements OnInit {

  PageState = PageState;
  state: PageState = PageState.Loading;
  readonly days = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

  strategies: ChargingStrategy[] = [];
  form: StrategyForm | null = null;
  saving = false;

  applying: ChargingStrategy | null = null;
  chargePoints: SmartChargePoint[] = [];
  stations: { id: number; name: string; count: number }[] = [];
  pointFilter = '';
  selectedPoints = new Set<number>();
  selectedStations = new Set<number>();
  applyRunning = false;
  applyResults: StrategyApplyResult[] | null = null;

  constructor(
    private _smartCharging: SmartChargingService,
    private _modal: ActionModalService
  ) { }

  ngOnInit() {
    this.loadStrategies();
  }

  loadStrategies() {
    this._smartCharging.getChargingStrategies().subscribe({
      next: strategies => {
        this.strategies = strategies;
        this.state = PageState.Success;
      },
      error: () => this.state = PageState.Error
    });
  }

  describe(strategy: ChargingStrategy): string {
    const kind = strategy.kind === 'Recurring' ? strategy.recurrencyKind : 'Absolute (from when applied)';
    return `${strategy.purpose} · ${kind} · ${strategy.chargingRateUnit} · stack ${strategy.stackLevel}`;
  }

  periodLabel(strategy: ChargingStrategy, period: ChargingStrategyPeriod): string {
    return this.startLabel(strategy.kind, strategy.recurrencyKind ?? null, period.startSeconds) + " → " + period.limit + " " + strategy.chargingRateUnit
      + (period.numberPhases ? " (" + period.numberPhases + "φ)" : "");
  }

  /** Share of the day / week covered by each period (the first one wraps from the end of the cycle). */
  timeline(strategy: ChargingStrategy): TimelineSegment[] {
    if (strategy.kind !== 'Recurring' || strategy.periods.length === 0) return [];
    const cycle = strategy.recurrencyKind === 'Weekly' ? WEEK : DAY;
    const sorted = [...strategy.periods].sort((a, b) => a.startSeconds - b.startSeconds);
    if (sorted[0].startSeconds !== 0) sorted.unshift({ ...sorted[sorted.length - 1], startSeconds: 0 });
    const max = Math.max(...sorted.map(p => p.limit), 1);
    return sorted.map((p, i) => {
      const end = i + 1 < sorted.length ? sorted[i + 1].startSeconds : cycle;
      return {
        widthPercent: (end - p.startSeconds) / cycle * 100,
        opacity: 0.15 + 0.85 * p.limit / max,
        title: this.startLabel(strategy.kind, strategy.recurrencyKind ?? null, p.startSeconds) + "–" + this.startLabel(strategy.kind, strategy.recurrencyKind ?? null, end % cycle)
          + ": " + p.limit + " " + strategy.chargingRateUnit
      };
    });
  }

  newStrategy() {
    this.applying = null;
    this.form = {
      id: null, name: '', description: '', purpose: 'TxDefaultProfile', kind: 'Recurring', recurrencyKind: 'Daily',
      chargingRateUnit: 'A', stackLevel: 0,
      periods: [{ day: 0, time: '00:00', offsetMinutes: 0, limit: 16, numberPhases: null }]
    };
  }

  editStrategy(strategy: ChargingStrategy) {
    this.applying = null;
    this.form = {
      id: strategy.id,
      name: strategy.name,
      description: strategy.description ?? '',
      purpose: strategy.purpose,
      kind: strategy.kind,
      recurrencyKind: strategy.recurrencyKind ?? 'Daily',
      chargingRateUnit: strategy.chargingRateUnit,
      stackLevel: strategy.stackLevel,
      periods: strategy.periods.map(p => ({
        day: Math.floor(p.startSeconds / DAY),
        time: this.toTime(p.startSeconds % DAY),
        offsetMinutes: Math.round(p.startSeconds / 60),
        limit: p.limit,
        numberPhases: p.numberPhases ?? null
      }))
    };
  }

  addPeriod() {
    const last = this.form?.periods[this.form.periods.length - 1];
    this.form?.periods.push({ day: last?.day ?? 0, time: '12:00', offsetMinutes: (last?.offsetMinutes ?? 0) + 60, limit: last?.limit ?? 16, numberPhases: last?.numberPhases ?? null });
  }

  removePeriod(index: number) {
    if (this.form && this.form.periods.length > 1) this.form.periods.splice(index, 1);
  }

  save() {
    if (!this.form) return;
    const f = this.form;
    const input: ChargingStrategyInput = {
      name: f.name,
      description: f.description || null,
      purpose: f.purpose,
      kind: f.kind,
      recurrencyKind: f.kind === 'Recurring' ? f.recurrencyKind : null,
      chargingRateUnit: f.chargingRateUnit,
      stackLevel: Number(f.stackLevel) || 0,
      periods: f.periods.map(p => ({
        startSeconds: this.toSeconds(f, p),
        limit: Number(p.limit),
        numberPhases: p.numberPhases ? Number(p.numberPhases) : null
      })).sort((a, b) => a.startSeconds - b.startSeconds)
    };
    this.saving = true;
    const request = f.id == null ? this._smartCharging.createChargingStrategy(input) : this._smartCharging.updateChargingStrategy(f.id, input);
    request.subscribe({
      next: () => {
        this.saving = false;
        this.form = null;
        this._modal.popup(ActionModalStatusEnum.Success, "Success !", "Strategy saved.", 3000);
        this.loadStrategies();
      },
      error: err => {
        this.saving = false;
        this.error(err);
      }
    });
  }

  deleteStrategy(strategy: ChargingStrategy) {
    if (!confirm(`Delete the strategy "${strategy.name}"? Profiles already sent stay on the chargers.`)) return;
    this._smartCharging.deleteChargingStrategy(strategy.id).subscribe({
      next: () => this.loadStrategies(),
      error: err => this.error(err)
    });
  }

  openApply(strategy: ChargingStrategy) {
    this.form = null;
    this.applying = strategy;
    this.applyResults = null;
    this.selectedPoints.clear();
    this.selectedStations.clear();
    if (this.chargePoints.length === 0) {
      this._smartCharging.getChargePoints().subscribe({
        next: points => {
          this.chargePoints = points;
          const byStation = new Map<number, { id: number; name: string; count: number }>();
          for (const p of points) {
            const station = byStation.get(p.chargingStationID) ?? { id: p.chargingStationID, name: p.chargingStationName, count: 0 };
            station.count++;
            byStation.set(p.chargingStationID, station);
          }
          this.stations = [...byStation.values()].sort((a, b) => a.name.localeCompare(b.name));
        },
        error: err => this.error(err)
      });
    }
  }

  get filteredPoints(): SmartChargePoint[] {
    const filter = this.pointFilter.trim().toLowerCase();
    return filter ? this.chargePoints.filter(p => (p.chargePointId + ' ' + p.chargingStationName).toLowerCase().includes(filter)) : this.chargePoints;
  }

  toggle(set: Set<number>, id: number) {
    if (set.has(id)) set.delete(id); else set.add(id);
  }

  apply() {
    if (!this.applying) return;
    this.applyRunning = true;
    this.applyResults = null;
    this._smartCharging.applyChargingStrategy(this.applying.id, [...this.selectedPoints], [...this.selectedStations]).subscribe({
      next: results => {
        this.applyRunning = false;
        this.applyResults = results;
        const accepted = results.filter(r => r.status === 'Accepted').length;
        const status = accepted === results.length ? ActionModalStatusEnum.Success : ActionModalStatusEnum.Warning;
        this._modal.popup(status, status === ActionModalStatusEnum.Success ? "Success !" : "Warning !", `${accepted} of ${results.length} charger(s) accepted the profile.`, 5000);
      },
      error: err => {
        this.applyRunning = false;
        this.error(err);
      }
    });
  }

  statusClass(status: string): string {
    switch (status) {
      case 'Accepted': return 'sc-badge sc-badge--ok';
      case 'Timeout': case 'NotConnected': return 'sc-badge sc-badge--warn';
      default: return 'sc-badge sc-badge--error';
    }
  }

  private startLabel(kind: ChargingProfileKind, recurrency: ChargingProfileRecurrency | null, seconds: number): string {
    if (kind !== 'Recurring') return "+" + formatOffset(seconds);
    if (recurrency === 'Weekly') return this.days[Math.floor(seconds / DAY) % 7].slice(0, 3) + " " + this.toTime(seconds % DAY);
    return this.toTime(seconds);
  }

  private toSeconds(form: StrategyForm, row: StrategyPeriodRow): number {
    if (form.kind !== 'Recurring') return Math.round(Number(row.offsetMinutes) * 60);
    const [h, m] = (row.time || '00:00').split(':').map(v => Number(v) || 0);
    const time = h * 3600 + m * 60;
    return form.recurrencyKind === 'Weekly' ? Number(row.day) * DAY + time : time;
  }

  private toTime(seconds: number): string {
    const h = Math.floor(seconds / 3600) % 24;
    const m = Math.floor((seconds % 3600) / 60);
    return String(h).padStart(2, '0') + ":" + String(m).padStart(2, '0');
  }

  private error(err: unknown) {
    this._modal.popup(ActionModalStatusEnum.Error, "Error !", apiErrorMessage(err), 5000);
  }
}
