import { Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { Subscription, distinctUntilChanged, filter, map } from 'rxjs';
import {
  ATTRIBUTE_TYPES, PendingProvisioningChargePoint, ProvisioningPlan, ProvisioningPlanVariable,
  ProvisioningResult, ProvisioningVariable, toVariableInput, variableLabel
} from 'src/_models/ocpp-provisioning';
import { AccessService } from 'src/_services/access.service';
import { ConfirmService } from 'src/_services/confirm.service';
import { NotificationService } from 'src/_services/notification.service';
import { OcppProvisioningService } from 'src/_services/ocpp-services/ocpp-provisioning.service';

type Step = 'choose' | 'discovering' | 'review' | 'applying' | 'done' | 'error';

interface ReviewRow {
  variable: ProvisioningPlanVariable;
  include: boolean;
  custom: boolean;
}

const AWAITING_PROVISIONING = 'ChargePointAwaitingProvisioning';

/**
 * Shown in the dashboard shell whenever a connected charge point has never been provisioned.
 * Until it is, the CSMS answers its BootNotification with Pending, so it cannot charge.
 */
@Component({
  selector: 'app-charge-point-provisioning',
  templateUrl: './charge-point-provisioning.component.html',
  styleUrls: ['./charge-point-provisioning.component.sass']
})
export class ChargePointProvisioningComponent implements OnInit, OnDestroy {

  pending: PendingProvisioningChargePoint[] = [];
  current: PendingProvisioningChargePoint | null = null;
  step: Step = 'choose';
  method: 'Automatic' | 'Manual' = 'Automatic';

  plan: ProvisioningPlan | null = null;
  rows: ReviewRow[] = [];
  result: ProvisioningResult | null = null;
  errorMessage = '';
  skippedBeforeSend = 0;

  newRow: ProvisioningVariable = this.emptyRow();
  showAddRow = false;

  readonly attributeTypes = ATTRIBUTE_TYPES;
  readonly label = variableLabel;

  // "Later" hides a charge point for the rest of this browser session.
  private readonly dismissed = new Set<string>();
  private subscriptions = new Subscription();

  constructor(
    private _provisioningService: OcppProvisioningService,
    private _notifications: NotificationService,
    private _access: AccessService,
    private _confirm: ConfirmService,
    private _router: Router
  ) { }

  ngOnInit() {
    // access.state re-emits on every route guard check; only a new sign-in should reload.
    this.subscriptions.add(this._access.state
      .pipe(filter(info => !!info), map(info => info!.userId), distinctUntilChanged())
      .subscribe(() => this.loadPending()));
    this.subscriptions.add(this._notifications.received$
      .pipe(filter(n => n.action === AWAITING_PROVISIONING))
      .subscribe(() => this.loadPending()));
    // The guided setup page handles its own charge point; do not pop up on top of it.
    this.subscriptions.add(this._router.events
      .pipe(filter(e => e instanceof NavigationEnd))
      .subscribe(() => {
        if (this.current && this.current.chargePointId === this.setupPageChargePointId() && (this.step === 'choose' || this.step === 'error')) this.showNext();
      }));
  }

  ngOnDestroy() {
    this.subscriptions.unsubscribe();
  }

  get canOperate(): boolean {
    return this._access.can('AccessDashboard') && this._access.can('OperateChargePoints');
  }

  get busy(): boolean {
    return this.step === 'discovering' || this.step === 'applying';
  }

  get remaining(): number {
    return this.pending.filter(cp => cp !== this.current && !this.dismissed.has(cp.chargePointId)).length;
  }

  // ------------------------------------------------------------ queue

  loadPending() {
    if (!this.canOperate || (this.current && this.step !== 'choose'))
      return;

    this._provisioningService.getPending().subscribe({
      next: list => {
        this.pending = list;
        if (!this.current || !list.some(cp => cp.chargePointId === this.current!.chargePointId))
          this.showNext();
      },
      // No access or API unavailable: stay out of the way.
      error: () => { }
    });
  }

  private showNext() {
    const onSetupPage = this.setupPageChargePointId();
    this.current = this.pending.find(cp => !this.dismissed.has(cp.chargePointId) && cp.chargePointId !== onSetupPage) ?? null;
    this.step = 'choose';
    this.plan = null;
    this.rows = [];
    this.result = null;
    this.errorMessage = '';
    this.showAddRow = false;
  }

  /** Hands this charge point to the full-page guided setup. */
  openGuidedSetup() {
    if (this.busy || !this.current) return;
    this._router.navigate(['/dashboard/charge-point-configurations', this.current.chargePointId, 'setup']);
  }

  private setupPageChargePointId(): string | null {
    const match = this._router.url.match(/\/charge-point-configurations\/([^/?#]+)\/setup/);
    return match ? decodeURIComponent(match[1]) : null;
  }

  later() {
    if (this.busy || !this.current) return;
    this.dismissed.add(this.current.chargePointId);
    this.showNext();
  }

  next() {
    if (this.current) this.pending = this.pending.filter(cp => cp !== this.current);
    this.showNext();
    this.loadPending();
  }

  @HostListener('document:keydown.escape')
  onEscape() {
    if (this.step === 'done') this.next();
    else if (this.step === 'choose' || this.step === 'error') this.later();
  }

  // ------------------------------------------------------------ flow

  start(method: 'Automatic' | 'Manual') {
    if (!this.current) return;
    this.method = method;
    this.step = 'discovering';
    this._provisioningService.discover(this.current.chargePointId).subscribe({
      next: plan => {
        this.plan = plan;
        this.rows = plan.variables.map(v => ({ variable: v, include: this.sendByDefault(v), custom: false }));
        if (method === 'Automatic')
          this.apply();
        else
          this.step = 'review';
      },
      error: error => this.fail(error)
    });
  }

  skip() {
    if (!this.current) return;
    const id = this.current.chargePointId;
    this._confirm.requestConfirmation(
      'Accept without configuring?',
      `${id} will be accepted with its factory settings. Offline charging, local authorization and other defaults stay as the manufacturer set them.`,
      () => {
        this.step = 'applying';
        this._provisioningService.skip(id).subscribe({
          next: result => { this.result = result; this.step = 'done'; },
          error: error => this.fail(error)
        });
      },
      { confirmLabel: 'Accept as-is', tone: 'danger' });
  }

  apply() {
    if (!this.current) return;
    const selected = this.rows.filter(r => r.include).map(r => r.variable);
    this.skippedBeforeSend = this.rows.length - selected.length;
    if (selected.length === 0) {
      this.errorMessage = 'Select at least one setting to send.';
      this.step = this.method === 'Manual' ? 'review' : 'error';
      return;
    }

    this.errorMessage = '';
    this.step = 'applying';
    this._provisioningService.apply(this.current.chargePointId, this.method, selected.map(toVariableInput)).subscribe({
      next: result => {
        this.result = result;
        this.step = 'done';
      },
      error: error => this.fail(error)
    });
  }

  retry() {
    this.errorMessage = '';
    if (this.method === 'Manual' && this.rows.length) this.step = 'review';
    else this.step = 'choose';
  }

  private fail(error: any) {
    this.errorMessage = error?.status === 409
      ? 'The charge point disconnected. It will show up again when it reconnects.'
      : error?.error?.error || error?.error?.message || 'Something went wrong while talking to the charge point.';
    this.step = 'error';
  }

  /** Skip what the charger said it cannot take; send the rest (including unknowns when there was no report). */
  private sendByDefault(v: ProvisioningPlanVariable): boolean {
    return v.support === 'Writable' || v.support === 'Unknown';
  }

  // ------------------------------------------------------------ manual review

  // Cached: a new array on every change-detection pass recreates the ngModel inputs, which
  // triggers another pass, forever.
  get groups(): { name: string; rows: ReviewRow[] }[] {
    if (this.rows === this.groupedRows && this.rows.length === this.groupedCount) return this.cachedGroups;
    const groups: { name: string; rows: ReviewRow[] }[] = [];
    for (const row of this.rows) {
      const name = row.variable.groupName || 'General';
      let group = groups.find(g => g.name === name);
      if (!group) groups.push(group = { name, rows: [] });
      group.rows.push(row);
    }
    this.groupedRows = this.rows;
    this.groupedCount = this.rows.length;
    this.cachedGroups = groups;
    return groups;
  }

  private groupedRows: ReviewRow[] | null = null;
  private groupedCount = 0;
  private cachedGroups: { name: string; rows: ReviewRow[] }[] = [];

  trackGroup = (_: number, group: { name: string }) => group.name;

  get selectedCount(): number {
    return this.rows.filter(r => r.include).length;
  }

  get autoSendCount(): number {
    return this.plan ? this.plan.variables.filter(v => this.sendByDefault(v)).length : 0;
  }

  isBoolean(v: ProvisioningPlanVariable): boolean {
    return v.dataType === 'boolean' || (!v.dataType && (v.value === 'true' || v.value === 'false'));
  }

  isNumeric(v: ProvisioningPlanVariable): boolean {
    return v.dataType === 'integer' || v.dataType === 'decimal';
  }

  /** Cached per variable: *ngFor needs the same array on every change-detection pass. */
  options(v: ProvisioningPlanVariable): string[] {
    let list = this.optionCache.get(v);
    if (!list) {
      list = v.dataType === 'OptionList' && v.valuesList ? v.valuesList.split(',').map(o => o.trim()).filter(Boolean) : [];
      this.optionCache.set(v, list);
    }
    return list;
  }

  private optionCache = new Map<ProvisioningPlanVariable, string[]>();

  supportLabel(v: ProvisioningPlanVariable): string {
    switch (v.support) {
      case 'Writable': return 'Supported';
      case 'ReadOnly': return 'Read-only';
      case 'NotReported': return 'Not reported';
      default: return 'Unknown';
    }
  }

  supportHint(v: ProvisioningPlanVariable): string {
    switch (v.support) {
      case 'ReadOnly': return 'The charger reports this variable as read-only; it would be rejected.';
      case 'NotReported': return 'The charger did not list this variable in its device model; it will probably answer UnknownVariable.';
      case 'Unknown': return 'No device-model report was available, so support could not be checked.';
      default: return 'The charger reports this variable as writable.';
    }
  }

  addRow() {
    const row = this.newRow;
    if (!row.componentName.trim() || !row.variableName.trim()) return;
    this.rows.push({
      variable: { ...row, componentName: row.componentName.trim(), variableName: row.variableName.trim(), groupName: 'Custom', support: 'Unknown' },
      include: true,
      custom: true
    });
    this.newRow = this.emptyRow();
    this.showAddRow = false;
  }

  removeRow(row: ReviewRow) {
    this.rows = this.rows.filter(r => r !== row);
  }

  private emptyRow(): ProvisioningVariable {
    return { groupName: 'Custom', componentName: '', componentInstance: '', evseId: null, connectorId: null, variableName: '', variableInstance: '', attributeType: 'Actual', value: '' };
  }

  // ------------------------------------------------------------ results

  statusTone(status: string): string {
    if (status === 'Accepted') return 'ok';
    if (status === 'RebootRequired') return 'warn';
    return 'bad';
  }

  get failedResults() {
    return this.result?.results.filter(r => this.statusTone(r.status) === 'bad') ?? [];
  }

  trackRow = (_: number, row: ReviewRow) => row.variable;
}
