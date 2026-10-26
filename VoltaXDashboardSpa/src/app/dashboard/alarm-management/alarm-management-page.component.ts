import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { ListFilterDef, changedFrom } from 'src/_models/list-filter';
import { FilterChipsComponent } from 'src/app/atoms/filter-chips/filter-chips.component';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, debounceTime, merge } from 'rxjs';
import { saveDownload } from 'src/_helpers/download';
import {
  ALARM_SEVERITIES, ALARM_STATUS_FILTERS, ALARM_TYPES, Alarm, AlarmAssignee, AlarmChange, AlarmFilter, AlarmPage, AlarmSummary, isActiveAlarm
} from 'src/_models/alarm';
import { ChargingStationForSelectDto } from 'src/_models/_dtos/charging-station-dtos/charging-station-for-select-dto';
import { AccessService } from 'src/_services/access.service';
import { AlarmService } from 'src/_services/alarm.service';
import { ChargingStationService } from 'src/_services/charging-station.service';
import { ToastService } from 'src/_services/toast.service';
import { PortalUiModule } from 'src/app/shared/portal-ui/portal-ui.module';

const EMPTY: AlarmFilter = {
  status: 'active', severity: '', type: '', chargingStationID: null, assigned: '', from: null, to: null, search: '', sort: '', page: 1, pageSize: 25
};

/**
 * Charge point alarms: active ones first (filters on status, severity, type, station, assignee, period and text),
 * kept current by the live feed, with the operator's actions (acknowledge, assign, resolve with a comment) in a
 * side panel. ?alarm=<id> opens one (links of the critical alarm notification and email).
 */
@Component({
  selector: 'app-alarm-management-page',
  standalone: true,
  imports: [PortalUiModule, FilterChipsComponent],
  templateUrl: './alarm-management-page.component.html',
  styleUrls: ['./alarm-management-page.component.sass']
})
export class AlarmManagementPageComponent implements OnInit {
  private readonly alarms = inject(AlarmService);
  private readonly stationsService = inject(ChargingStationService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  readonly access = inject(AccessService);

  readonly severities = ALARM_SEVERITIES;
  readonly types = ALARM_TYPES;
  readonly statuses = ALARM_STATUS_FILTERS;

  filter: AlarmFilter = { ...EMPTY };
  /** The filters of the last load that differ from the defaults, shown as removable chips. */
  applied: Record<string, any> = {};
  chipDefs: ListFilterDef[] = [];
  page: AlarmPage | null = null;
  summary: AlarmSummary | null = null;
  stations: ChargingStationForSelectDto[] = [];
  assignees: AlarmAssignee[] | null = null;
  loading = false;
  error = false;
  exporting = false;
  /** Changes pushed while the list was not on its first page / default order: offered as a reload. */
  pendingChanges = 0;

  selected: Alarm | null = null;
  detailLoading = false;
  detailError = false;
  busy: 'acknowledge' | 'assign' | 'resolve' | null = null;
  assignTo: number | null = null;
  comment = '';
  commentTouched = false;

  private readonly reload$ = new Subject<void>();

  get canEdit(): boolean { return this.access.can('EditAlarms'); }
  get commentInvalid(): boolean { return this.comment.trim().length < 3; }
  get myId(): number | null { return this.access.current?.userId ?? null; }

  ngOnInit(): void {
    this.chipDefs = this.buildChipDefs();
    const params = this.route.snapshot.queryParamMap;
    const station = Number(params.get('station'));
    if (Number.isInteger(station) && station > 0) this.filter.chargingStationID = station;
    const chargePoint = Number(params.get('chargePoint'));
    if (Number.isInteger(chargePoint) && chargePoint > 0) this.filter.chargePointID = chargePoint;

    this.load();
    this.loadSummary();
    this.stationsService.getChargingStationsForSelect().pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: stations => { this.stations = stations ?? []; this.chipDefs = this.buildChipDefs(); },
        error: () => this.stations = []
      });

    const alarmId = Number(params.get('alarm'));
    if (Number.isInteger(alarmId) && alarmId > 0) this.open(alarmId);

    // Live feed: the open alarm follows its changes; the list reloads once per burst.
    this.alarms.changes().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(change => this.onChange(change));
    merge(this.reload$, this.alarms.reconnected()).pipe(debounceTime(800), takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.load(this.filter.page, true);
      this.loadSummary();
    });
  }

  load(page = 1, silent = false): void {
    this.filter.page = page;
    this.applied = changedFrom(this.filter, EMPTY);
    if (!silent) this.loading = true;
    this.pendingChanges = 0;
    this.alarms.list(this.filter).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => { this.page = result; this.loading = false; this.error = false; },
      error: () => { this.loading = false; this.error = true; }
    });
  }

  loadSummary(): void {
    this.alarms.summary().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ next: s => this.summary = s, error: () => this.summary = null });
  }

  search(): void { this.load(1); }

  reset(): void {
    this.filter = { ...EMPTY };
    this.load(1);
  }

  /** A chip was removed: that filter goes back to its default and the list reloads. */
  removeFilter(key: string): void {
    (this.filter as any)[key] = (EMPTY as any)[key];
    this.load(1);
  }

  private buildChipDefs(): ListFilterDef[] {
    return [
      { key: 'status', label: 'common.fields.status', type: 'select', options: this.statuses.map(s => ({ value: s, label: 'alarms.statusFilter.' + s })) },
      { key: 'severity', label: 'alarms.fields.severity', type: 'select', options: this.severities.map(s => ({ value: s, label: 'alarms.severity.' + s })) },
      { key: 'type', label: 'alarms.fields.type', type: 'select', options: this.types.map(t => ({ value: t, label: 'alarms.type.' + t })) },
      { key: 'chargingStationID', label: 'alarms.fields.station', type: 'select', options: this.stations.map(s => ({ value: s.id, label: s.name })) },
      { key: 'assigned', label: 'alarms.fields.assignedTo', type: 'select', options: [
        { value: 'me', label: 'alarms.assigned.me' }, { value: 'none', label: 'alarms.assigned.none' }] },
      { key: 'from', label: 'common.fields.from', type: 'date' },
      { key: 'to', label: 'common.fields.to', type: 'date' },
      { key: 'search', label: 'common.fields.search', type: 'text' },
    ];
  }

  /** Summary tiles filter the list. */
  quickFilter(kind: 'active' | 'critical' | 'unassigned' | 'mine'): void {
    this.filter = { ...EMPTY, severity: kind === 'critical' ? 'Critical' : '', assigned: kind === 'unassigned' ? 'none' : kind === 'mine' ? 'me' : '' };
    this.load(1);
  }

  export(): void {
    this.exporting = true;
    this.alarms.export(this.filter).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: response => { saveDownload(response, 'eveon-alarms.csv'); this.exporting = false; },
      error: error => { this.exporting = false; this.toast.apiError(error, 'alarms.toast.exportFailed'); }
    });
  }

  open(id: number): void {
    this.detailLoading = true;
    this.detailError = false;
    this.selected = this.page?.items.find(a => a.id === id) ?? null;
    this.resetForms();
    this.alarms.get(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: alarm => { this.select(alarm); this.detailLoading = false; },
      error: () => { this.detailLoading = false; this.detailError = true; }
    });
    if (this.canEdit && this.assignees == null)
      this.alarms.assignees().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ next: a => this.assignees = a, error: () => this.assignees = [] });
    this.router.navigate([], { queryParams: { alarm: id }, queryParamsHandling: 'merge', replaceUrl: true });
  }

  close(): void {
    this.selected = null;
    this.detailError = false;
    this.router.navigate([], { queryParams: { alarm: null }, queryParamsHandling: 'merge', replaceUrl: true });
  }

  acknowledge(alarm: Alarm): void {
    this.act('acknowledge', this.alarms.acknowledge(alarm.id), 'alarms.toast.acknowledged');
  }

  assign(alarm: Alarm, userId: number | null = this.assignTo): void {
    this.act('assign', this.alarms.assign(alarm.id, userId), userId == null ? 'alarms.toast.unassigned' : 'alarms.toast.assigned');
  }

  takeIt(alarm: Alarm): void {
    if (this.myId != null) this.assign(alarm, this.myId);
  }

  resolve(alarm: Alarm): void {
    this.commentTouched = true;
    if (this.commentInvalid) return;
    this.act('resolve', this.alarms.resolve(alarm.id, this.comment.trim()), 'alarms.toast.resolved');
  }

  isActive(alarm: Alarm): boolean { return isActiveAlarm(alarm); }

  severityTone(alarm: Pick<Alarm, 'severity'>): string {
    switch (alarm.severity) {
      case 'Critical': return 'vx-badge--danger';
      case 'Major': return 'vx-badge--warning';
      default: return 'vx-badge--info';
    }
  }

  statusTone(alarm: Pick<Alarm, 'status'>): string {
    switch (alarm.status) {
      case 'Open': return 'vx-badge--danger';
      case 'Acknowledged': return 'vx-badge--warning';
      default: return 'vx-badge--success';
    }
  }

  /** "EVSE 1 · connector 2", "EVSE 1" or the whole charge point. */
  scope(alarm: Alarm): { key: string; params: object } {
    if (alarm.evseId == null) return { key: 'alarms.scope.chargePoint', params: {} };
    return alarm.connectorNumber == null
      ? { key: 'alarms.scope.evse', params: { evse: alarm.evseId } }
      : { key: 'alarms.scope.connector', params: { evse: alarm.evseId, connector: alarm.connectorNumber } };
  }

  private act(kind: 'acknowledge' | 'assign' | 'resolve', request: ReturnType<AlarmService['get']>, successKey: string): void {
    this.busy = kind;
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: alarm => {
        this.busy = null;
        this.select(alarm);
        this.replaceRow(alarm);
        this.toast.success(successKey);
        this.loadSummary();
      },
      error: error => {
        this.busy = null;
        // 409: closed meanwhile (cleared by the charger or resolved by someone else): show what it is now.
        if (error?.status === 409 && this.selected) this.open(this.selected.id);
        this.toast.apiError(error, 'alarms.toast.actionFailed');
      }
    });
  }

  private select(alarm: Alarm): void {
    const changed = this.selected?.id !== alarm.id;
    this.selected = alarm;
    if (changed) this.resetForms();
    this.assignTo = alarm.assignedToID;
  }

  private resetForms(): void {
    this.comment = '';
    this.commentTouched = false;
    this.assignTo = this.selected?.assignedToID ?? null;
  }

  private replaceRow(alarm: Alarm): void {
    if (!this.page) return;
    this.page = { ...this.page, items: this.page.items.map(a => a.id === alarm.id ? alarm : a) };
  }

  private onChange(change: AlarmChange): void {
    if (this.selected?.id === change.alarm.id) {
      this.selected = change.alarm;
      if (this.assignTo == null) this.assignTo = change.alarm.assignedToID;
    }
    const shown = this.page?.items.some(a => a.id === change.alarm.id);
    if (shown) this.replaceRow(change.alarm);
    // New alarms and status changes move rows: reload the first page, or tell the user on other pages.
    if (this.filter.page === 1) this.reload$.next();
    else if (!shown) this.pendingChanges++;
    if (this.filter.page !== 1) this.loadSummary();
  }
}
