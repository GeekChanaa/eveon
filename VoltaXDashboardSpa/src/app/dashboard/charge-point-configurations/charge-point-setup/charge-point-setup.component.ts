import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subscription, timer, switchMap, takeWhile } from 'rxjs';
import { PageState } from 'src/_models/_enums/page-state.enum';
import { ChargePointConfigurationStatus } from 'src/_models/charge-point-configuration';
import {
  ProvisioningPlan, ProvisioningPlanVariable, ProvisioningResult, toVariableInput, variableLabel
} from 'src/_models/ocpp-provisioning';
import { AccessService } from 'src/_services/access.service';
import { ConfirmService } from 'src/_services/confirm.service';
import { OcppDeviceModelService } from 'src/_services/ocpp-services/ocpp-device-model.service';
import { OcppProvisioningService } from 'src/_services/ocpp-services/ocpp-provisioning.service';

type StepKey = 'connect' | 'identify' | 'discover' | 'review' | 'apply' | 'finish';

interface Step {
  key: StepKey;
  title: string;
  hint: string;
}

interface ReviewRow {
  variable: ProvisioningPlanVariable;
  include: boolean;
}

interface ReviewGroup {
  name: string;
  rows: ReviewRow[];
}

// Short pauses so each finished step is seen before the next one starts.
const STEP_PAUSE = 900;
const POLL_INTERVAL = 3000;

/**
 * Guided first-time setup of an OCPP 2.0.1 charge point: wait for its connection, show what it
 * reported on boot, read its device model (GetBaseReport), then send the default configuration
 * (SetVariables) and let it leave the Pending state.
 */
@Component({
  selector: 'app-charge-point-setup',
  templateUrl: './charge-point-setup.component.html',
  styleUrls: ['./charge-point-setup.component.sass']
})
export class ChargePointSetupComponent implements OnInit, OnDestroy {

  readonly steps: Step[] = [
    { key: 'connect', title: 'Connect', hint: 'Reach the charge point over OCPP' },
    { key: 'identify', title: 'Identify', hint: 'What it reported on boot' },
    { key: 'discover', title: 'Read device model', hint: 'GetBaseReport · FullInventory' },
    { key: 'review', title: 'Review', hint: 'Default configuration' },
    { key: 'apply', title: 'Apply', hint: 'SetVariables' },
    { key: 'finish', title: 'Done', hint: 'Ready to charge' }
  ];

  readonly label = variableLabel;
  readonly PageState = PageState;

  chargePointId = '';
  stepIndex = 0;
  failed = false;
  errorMessage = '';

  status: ChargePointConfigurationStatus | null = null;
  waitingForConnection = false;
  elapsed = 0;

  discoverHint = 0;
  readonly discoverHints = [
    'Asking the charger for its full inventory…',
    'Receiving NotifyReport messages…',
    'Storing every component and variable…',
    'Matching the default configuration against what it supports…'
  ];

  plan: ProvisioningPlan | null = null;
  rows: ReviewRow[] = [];
  groups: ReviewGroup[] = [];
  expandedGroup: string | null = null;
  counts = { send: 0, readOnly: 0, notReported: 0 };

  result: ProvisioningResult | null = null;
  revealed = 0;
  skipping = false;

  private initialSelection = '';
  private subscriptions = new Subscription();
  private timers: ReturnType<typeof setTimeout>[] = [];
  private clock?: ReturnType<typeof setInterval>;

  constructor(
    private _route: ActivatedRoute,
    private _deviceModel: OcppDeviceModelService,
    private _provisioning: OcppProvisioningService,
    private _confirm: ConfirmService,
    public access: AccessService
  ) { }

  ngOnInit() {
    this.chargePointId = this._route.snapshot.paramMap.get('chargePointId') ?? '';
    this.connect();
  }

  ngOnDestroy() {
    this.subscriptions.unsubscribe();
    this.timers.forEach(clearTimeout);
    this.stopClock();
  }

  get step(): StepKey {
    return this.steps[this.stepIndex].key;
  }

  get progress(): number {
    return (this.stepIndex / (this.steps.length - 1)) * 100;
  }

  get canOperate(): boolean {
    return this.access.can('OperateChargePoints');
  }

  stepState(index: number): 'done' | 'active' | 'error' | 'upcoming' {
    if (index < this.stepIndex || (index === this.stepIndex && this.step === 'finish')) return 'done';
    if (index === this.stepIndex) return this.failed ? 'error' : 'active';
    return 'upcoming';
  }

  // ------------------------------------------------------------ 1. connect

  connect() {
    this.goTo('connect');
    this.waitingForConnection = false;
    this.startClock();

    // Polls until the charger is connected; the first answer arrives right away.
    const poll = timer(0, POLL_INTERVAL).pipe(
      switchMap(() => this._deviceModel.status(this.chargePointId)),
      takeWhile(status => !status.isOnline, true)
    ).subscribe({
      next: status => {
        this.status = status;
        this.waitingForConnection = !status.isOnline;
        if (status.isOnline) {
          this.stopClock();
          this.later(() => this.identify());
        }
      },
      error: error => this.fail(error?.status === 404
        ? `There is no charge point with the id ${this.chargePointId}.`
        : 'The charge point status could not be loaded.')
    });
    this.subscriptions.add(poll);
  }

  // ------------------------------------------------------------ 2. identify

  private identify() {
    this.goTo('identify');
    // Long enough to read the identity card as it animates in.
    this.later(() => this.discover(), STEP_PAUSE + 1400);
  }

  // ------------------------------------------------------------ 3. discover

  discover() {
    this.goTo('discover');
    this.discoverHint = 0;
    this.startClock(() => {
      if (this.elapsed % 4 === 0) this.discoverHint = Math.min(this.discoverHint + 1, this.discoverHints.length - 1);
    });

    this.subscriptions.add(this._provisioning.discover(this.chargePointId).subscribe({
      next: plan => {
        this.stopClock();
        this.setPlan(plan);
        this.later(() => this.goTo('review'));
      },
      error: error => this.fail(this.httpMessage(error))
    }));
  }

  private setPlan(plan: ProvisioningPlan) {
    this.plan = plan;
    this.rows = plan.variables.map(v => ({ variable: v, include: v.support === 'Writable' || v.support === 'Unknown' }));
    const groups: ReviewGroup[] = [];
    for (const row of this.rows) {
      const name = row.variable.groupName || 'General';
      let group = groups.find(g => g.name === name);
      if (!group) groups.push(group = { name, rows: [] });
      group.rows.push(row);
    }
    this.groups = groups;
    this.counts = {
      send: this.rows.filter(r => r.include).length,
      readOnly: plan.variables.filter(v => v.support === 'ReadOnly').length,
      notReported: plan.variables.filter(v => v.support === 'NotReported').length
    };
    this.initialSelection = this.selectionSnapshot();
  }

  // ------------------------------------------------------------ 4. review

  get selectedCount(): number {
    return this.rows.filter(r => r.include).length;
  }

  groupSelected(group: ReviewGroup): number {
    return group.rows.filter(r => r.include).length;
  }

  toggleGroup(group: ReviewGroup) {
    this.expandedGroup = this.expandedGroup === group.name ? null : group.name;
  }

  isBoolean(v: ProvisioningPlanVariable): boolean {
    return v.dataType?.toLowerCase() === 'boolean' || (!v.dataType && (v.value === 'true' || v.value === 'false'));
  }

  /** Cached per variable: *ngFor needs the same array on every change-detection pass. */
  options(v: ProvisioningPlanVariable): string[] {
    let list = this.optionCache.get(v);
    if (!list) {
      list = v.dataType?.toLowerCase() === 'optionlist' && v.valuesList ? v.valuesList.split(',').map(o => o.trim()).filter(Boolean) : [];
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
      default: return 'Unchecked';
    }
  }

  skip() {
    this._confirm.requestConfirmation(
      'Accept without configuring?',
      `${this.chargePointId} will be accepted with its factory settings and no setting will be sent.`,
      () => {
        this.skipping = true;
        this.subscriptions.add(this._provisioning.skip(this.chargePointId).subscribe({
          next: result => {
            this.skipping = false;
            this.result = result;
            this.revealed = 0;
            this.goTo('finish');
          },
          error: error => { this.skipping = false; this.fail(this.httpMessage(error)); }
        }));
      },
      { confirmLabel: 'Accept as-is', tone: 'danger' });
  }

  // ------------------------------------------------------------ 5. apply

  apply() {
    const selected = this.rows.filter(r => r.include).map(r => r.variable);
    if (selected.length === 0) return;
    // Untouched defaults count as an automatic setup; any change makes it a manual one.
    const method = this.selectionSnapshot() === this.initialSelection ? 'Automatic' : 'Manual';

    this.goTo('apply');
    this.result = null;
    this.revealed = 0;
    this.startClock();
    this.subscriptions.add(this._provisioning.apply(this.chargePointId, method, selected.map(toVariableInput)).subscribe({
      next: result => {
        this.stopClock();
        this.result = result;
        if (!result.provisioned) {
          this.fail(result.message || 'The charger did not accept the configuration.');
          return;
        }
        this.revealResults(result);
      },
      error: error => this.fail(this.httpMessage(error))
    }));
  }

  /** Shows result rows one after another, then moves on. */
  private revealResults(result: ProvisioningResult) {
    const total = result.results.length;
    const perRow = Math.max(25, Math.min(90, 1400 / Math.max(total, 1)));
    for (let i = 1; i <= total; i++) this.later(() => this.revealed = i, i * perRow);
    this.later(() => this.goTo('finish'), total * perRow + STEP_PAUSE + 600);
  }

  statusTone(status: string): string {
    if (status === 'Accepted') return 'ok';
    if (status === 'RebootRequired') return 'warn';
    return 'bad';
  }

  get failedResults() {
    return this.result?.results.filter(r => this.statusTone(r.status) === 'bad') ?? [];
  }

  // ------------------------------------------------------------ errors / retry

  retry() {
    const step = this.step;
    this.failed = false;
    this.errorMessage = '';
    if (step === 'connect' || step === 'identify') this.connect();
    else if (step === 'discover') this.discover();
    else if (step === 'apply') this.goTo('review');
    else this.connect();
  }

  private fail(message: string) {
    this.stopClock();
    this.failed = true;
    this.errorMessage = message;
  }

  private httpMessage(error: any): string {
    if (error?.status === 409) return 'The charge point disconnected. Reconnect it, then try again.';
    if (error?.status === 403) return 'You need the Operate Charge Points permission to configure charge points.';
    return error?.error?.error || error?.error?.message || 'Something went wrong while talking to the charge point.';
  }

  // ------------------------------------------------------------ helpers

  private goTo(step: StepKey) {
    this.failed = false;
    this.errorMessage = '';
    this.stepIndex = this.steps.findIndex(s => s.key === step);
  }

  private later(action: () => void, delay = STEP_PAUSE) {
    this.timers.push(setTimeout(action, delay));
  }

  private startClock(onTick?: () => void) {
    this.stopClock();
    this.elapsed = 0;
    this.clock = setInterval(() => { this.elapsed++; onTick?.(); }, 1000);
  }

  private stopClock() {
    if (this.clock) clearInterval(this.clock);
    this.clock = undefined;
  }

  private selectionSnapshot(): string {
    return JSON.stringify(this.rows.map(r => [r.include, r.variable.value]));
  }

  trackRow = (_: number, row: ReviewRow) => row.variable;
  trackGroup = (_: number, group: ReviewGroup) => group.name;
}
