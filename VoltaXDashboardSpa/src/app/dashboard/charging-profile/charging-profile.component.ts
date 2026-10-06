import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { PageState } from 'src/_models/_enums/page-state.enum';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { showOcppCommandFeedback } from 'src/_services/ocpp-services/ocpp-command-feedback';
import {
  apiErrorMessage, ChargingProfile, ChargingProfileInput, ChargingProfileKind, ChargingProfilePurpose, ChargingProfileRecurrency,
  ChargingRateUnit, CompositeSchedule, EvChargingNeeds, formatOffset, SmartChargePoint, SmartChargingService
} from 'src/_services/smart-charging.service';

interface PeriodRow {
  startMinutes: number;
  limit: number;
  numberPhases: number | null;
}

interface ProfileForm {
  chargingProfileID: number | null;
  purpose: ChargingProfilePurpose;
  evseId: number;
  stackLevel: number;
  kind: ChargingProfileKind;
  recurrencyKind: ChargingProfileRecurrency;
  startSchedule: string;
  durationMinutes: number | null;
  chargingRateUnit: ChargingRateUnit;
  validFrom: string;
  validTo: string;
  periods: PeriodRow[];
}

@Component({
  selector: 'app-charging-profile',
  templateUrl: './charging-profile.component.html',
  styleUrls: ['../smart-charging-shared.css', './charging-profile.component.css']
})
export class ChargingProfileComponent implements OnInit {

  PageState = PageState;
  state: PageState = PageState.Loading;
  formatOffset = formatOffset;

  chargePoints: SmartChargePoint[] = [];
  selected: SmartChargePoint | null = null;
  selectedChargePointId: number | null = null;
  evseFilter: number | null = null;

  profiles: ChargingProfile[] = [];
  includeCleared = false;
  loadingProfiles = false;
  evNeeds: EvChargingNeeds[] = [];

  form: ProfileForm | null = null;
  sending = false;

  compositeEvse = 0;
  compositeDuration = 86400;
  compositeUnit: ChargingRateUnit | '' = '';
  composite: CompositeSchedule | null = null;
  compositeLoading = false;
  compositeChart: any = null;

  readonly durations = [
    { label: '1 hour', value: 3600 },
    { label: '6 hours', value: 21600 },
    { label: '24 hours', value: 86400 },
    { label: '48 hours', value: 172800 }
  ];

  constructor(
    private _smartCharging: SmartChargingService,
    private _modal: ActionModalService
  ) { }

  ngOnInit() {
    this._smartCharging.getChargePoints().subscribe({
      next: points => {
        this.chargePoints = points;
        this.state = PageState.Success;
      },
      error: () => this.state = PageState.Error
    });
  }

  get evseOptions(): number[] {
    return [0, ...(this.selected?.evseIds ?? []).filter(e => e > 0)];
  }

  get visibleProfiles(): ChargingProfile[] {
    return this.evseFilter == null ? this.profiles : this.profiles.filter(p => p.evseId === this.evseFilter);
  }

  get isOcpp201(): boolean {
    return this.selected?.protocol === 'ocpp2.0.1';
  }

  selectChargePoint() {
    this.selected = this.chargePoints.find(c => c.id === Number(this.selectedChargePointId)) ?? null;
    this.evseFilter = null;
    this.form = null;
    this.composite = null;
    this.compositeChart = null;
    this.compositeEvse = 0;
    this.profiles = [];
    this.evNeeds = [];
    if (this.selected) {
      this.loadProfiles();
      this._smartCharging.getEvChargingNeeds(this.selected.id).subscribe({ next: needs => this.evNeeds = needs, error: () => this.evNeeds = [] });
    }
  }

  loadProfiles() {
    if (!this.selected) return;
    this.loadingProfiles = true;
    this._smartCharging.getChargingProfiles(this.selected.id, this.includeCleared).subscribe({
      next: profiles => {
        this.profiles = profiles;
        this.loadingProfiles = false;
      },
      error: err => {
        this.loadingProfiles = false;
        this.error(err);
      }
    });
  }

  newProfile(purpose: ChargingProfilePurpose) {
    this.form = {
      chargingProfileID: null,
      purpose,
      evseId: purpose === 'ChargingStationMaxProfile' ? 0 : (this.evseFilter ?? 0),
      stackLevel: 0,
      kind: 'Absolute',
      recurrencyKind: 'Daily',
      startSchedule: '',
      durationMinutes: null,
      chargingRateUnit: 'A',
      validFrom: '',
      validTo: '',
      periods: [{ startMinutes: 0, limit: purpose === 'ChargingStationMaxProfile' ? 32 : 16, numberPhases: null }]
    };
  }

  editProfile(profile: ChargingProfile) {
    this.form = {
      chargingProfileID: profile.id,
      purpose: profile.purpose,
      evseId: profile.evseId,
      stackLevel: profile.stackLevel,
      kind: profile.kind,
      recurrencyKind: profile.recurrencyKind ?? 'Daily',
      startSchedule: this.toLocalInput(profile.startSchedule),
      durationMinutes: profile.duration ? Math.round(profile.duration / 60) : null,
      chargingRateUnit: profile.chargingRateUnit,
      validFrom: this.toLocalInput(profile.validFrom),
      validTo: this.toLocalInput(profile.validTo),
      periods: profile.periods.map(p => ({ startMinutes: Math.round(p.startPeriod / 60), limit: p.limit, numberPhases: p.numberPhases ?? null }))
    };
  }

  canEdit(profile: ChargingProfile): boolean {
    return profile.status !== 'Cleared' && (profile.purpose === 'TxDefaultProfile' || profile.purpose === 'ChargingStationMaxProfile')
      && profile.source !== 'ChargerReported';
  }

  canClear(profile: ChargingProfile): boolean {
    return profile.status !== 'Cleared' && profile.purpose !== 'ChargingStationExternalConstraints';
  }

  addPeriod() {
    if (!this.form) return;
    const last = this.form.periods[this.form.periods.length - 1];
    this.form.periods.push({ startMinutes: (last?.startMinutes ?? 0) + 60, limit: last?.limit ?? 16, numberPhases: last?.numberPhases ?? null });
  }

  removePeriod(index: number) {
    if (this.form && this.form.periods.length > 1) this.form.periods.splice(index, 1);
  }

  onPurposeChange() {
    if (this.form?.purpose === 'ChargingStationMaxProfile') this.form.evseId = 0;
  }

  sendProfile() {
    if (!this.form || !this.selected) return;
    const f = this.form;
    const periods = [...f.periods]
      .sort((a, b) => a.startMinutes - b.startMinutes)
      .map(p => ({ startPeriod: Math.round(Number(p.startMinutes) * 60), limit: Number(p.limit), numberPhases: p.numberPhases ? Number(p.numberPhases) : null }));
    const input: ChargingProfileInput = {
      chargingProfileID: f.chargingProfileID,
      purpose: f.purpose,
      evseId: Number(f.evseId),
      stackLevel: Number(f.stackLevel),
      kind: f.kind,
      recurrencyKind: f.kind === 'Recurring' ? f.recurrencyKind : null,
      startSchedule: f.kind === 'Relative' ? null : this.fromLocalInput(f.startSchedule),
      duration: f.durationMinutes ? Math.round(Number(f.durationMinutes) * 60) : null,
      chargingRateUnit: f.chargingRateUnit,
      validFrom: this.fromLocalInput(f.validFrom),
      validTo: this.fromLocalInput(f.validTo),
      periods
    };
    this.sending = true;
    this._smartCharging.sendChargingProfile(this.selected.chargePointId, input).subscribe({
      next: answer => {
        this.sending = false;
        if (showOcppCommandFeedback(this._modal, answer)) this.form = null;
        this.loadProfiles();
      },
      error: err => {
        this.sending = false;
        this.commandError(err);
        this.loadProfiles();
      }
    });
  }

  clearProfile(profile: ChargingProfile) {
    if (!this.selected) return;
    this._smartCharging.clearChargingProfile(this.selected.chargePointId, profile.id).subscribe({
      next: answer => {
        showOcppCommandFeedback(this._modal, answer);
        this.loadProfiles();
      },
      error: err => this.commandError(err)
    });
  }

  requestReport() {
    if (!this.selected) return;
    this._smartCharging.requestChargingProfilesReport(this.selected.chargePointId, this.evseFilter).subscribe({
      next: answer => {
        showOcppCommandFeedback(this._modal, answer);
        // The charger reports in separate messages after answering.
        setTimeout(() => this.loadProfiles(), 3000);
      },
      error: err => this.commandError(err)
    });
  }

  loadComposite() {
    if (!this.selected) return;
    this.compositeLoading = true;
    this.composite = null;
    this.compositeChart = null;
    this._smartCharging.getCompositeSchedule(this.selected.chargePointId, Number(this.compositeEvse), Number(this.compositeDuration),
      this.compositeUnit === '' ? null : this.compositeUnit).subscribe({
      next: answer => {
        this.compositeLoading = false;
        showOcppCommandFeedback(this._modal, answer);
        this.composite = answer.response;
        if (answer.response?.status === 'Accepted') this.compositeChart = this.buildChart(answer.response);
      },
      error: err => {
        this.compositeLoading = false;
        this.commandError(err);
      }
    });
  }

  periodsSummary(profile: ChargingProfile): string {
    return profile.periods.map(p => formatOffset(p.startPeriod) + " → " + p.limit + " " + profile.chargingRateUnit + (p.numberPhases ? " (" + p.numberPhases + "φ)" : "")).join(", ");
  }

  statusClass(status: string | null | undefined): string {
    switch (status) {
      case 'Accepted': return 'sc-badge sc-badge--ok';
      case 'Rejected': return 'sc-badge sc-badge--error';
      case 'Pending': return 'sc-badge sc-badge--warn';
      default: return 'sc-badge';
    }
  }

  private buildChart(schedule: CompositeSchedule) {
    const start = schedule.scheduleStart ? new Date(schedule.scheduleStart).getTime() : Date.now();
    const end = start + (schedule.duration ?? this.compositeDuration) * 1000;
    const points = [...schedule.periods]
      .sort((a, b) => a.startPeriod - b.startPeriod)
      .map(p => ({ x: start + p.startPeriod * 1000, y: p.limit }));
    if (points.length > 0) points.push({ x: end, y: points[points.length - 1].y });
    return {
      series: [{ name: 'Limit (' + (schedule.chargingRateUnit ?? '') + ')', data: points }],
      chart: { height: 280, type: 'line', toolbar: { show: false }, zoom: { enabled: false }, fontFamily: 'Inter, sans-serif', animations: { enabled: false } },
      stroke: { curve: 'stepline', width: 3 },
      dataLabels: { enabled: false },
      xaxis: { type: 'datetime', labels: { datetimeUTC: false } },
      yaxis: { min: 0, title: { text: schedule.chargingRateUnit ?? '' } },
      tooltip: { x: { format: 'dd MMM HH:mm' } },
      grid: { strokeDashArray: 4 },
      colors: ['#2A85FF'],
      legend: { show: false }
    };
  }

  private toLocalInput(value: string | null | undefined): string {
    if (!value) return '';
    const date = new Date(value);
    const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
    return local.toISOString().slice(0, 16);
  }

  private fromLocalInput(value: string): string | null {
    return value ? new Date(value).toISOString() : null;
  }

  private commandError(err: unknown) {
    if (err instanceof HttpErrorResponse && (err.status === 409 || err.status === 502 || err.status === 504)) {
      showOcppCommandFeedback(this._modal, err);
    } else {
      this.error(err);
    }
  }

  private error(err: unknown) {
    this._modal.popup(ActionModalStatusEnum.Error, "Error !", apiErrorMessage(err), 5000);
  }
}
