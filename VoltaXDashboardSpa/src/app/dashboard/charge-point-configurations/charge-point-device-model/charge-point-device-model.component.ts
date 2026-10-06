import { Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subscription, interval, switchMap } from 'rxjs';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { PageState } from 'src/_models/_enums/page-state.enum';
import {
  ChargePointConfigurationStatus, DeviceModelAttribute, DeviceModelVariable, ReportBase, attributeKey, componentKey, evseLabel
} from 'src/_models/charge-point-configuration';
import { ProvisioningVariable } from 'src/_models/ocpp-provisioning';
import { AccessService } from 'src/_services/access.service';
import { ActionModalService } from 'src/_services/action-modal.service';
import { ConfirmService } from 'src/_services/confirm.service';
import { OcppDeviceModelService } from 'src/_services/ocpp-services/ocpp-device-model.service';

/** One attribute (Actual, Target, MinSet, MaxSet) of one device-model variable. */
interface AttributeRow {
  key: string;
  variable: DeviceModelVariable;
  attribute: DeviceModelAttribute;
  writable: boolean;
  search: string;
  // Allowed values of an OptionList, computed once so *ngFor gets a stable array.
  choices: string[];
}

interface ComponentEntry {
  key: string;
  name: string;
  detail: string;
  rows: AttributeRow[];
  writableCount: number;
}

interface RowResult {
  status: string;
  reason?: string | null;
  tone: 'ok' | 'warn' | 'bad';
}

// Beyond this, a search shows the first matches only; the DOM stays light.
const MAX_ROWS = 300;

/**
 * Every component and variable a charge point reported in its OCPP 2.0.1 device model, editable
 * with SetVariables, refreshable with GetBaseReport and readable live with GetVariables.
 */
@Component({
  selector: 'app-charge-point-device-model',
  templateUrl: './charge-point-device-model.component.html',
  styleUrls: ['./charge-point-device-model.component.sass']
})
export class ChargePointDeviceModelComponent implements OnInit, OnDestroy {

  PageState = PageState;
  state: PageState = PageState.Loading;
  readonly evseLabel = evseLabel;
  readonly reportBases: { key: ReportBase; label: string; hint: string }[] = [
    { key: 'FullInventory', label: 'Full inventory', hint: 'Every component and variable, with limits and allowed values' },
    { key: 'ConfigurationInventory', label: 'Configuration only', hint: 'Only the variables that can be configured' },
    { key: 'SummaryInventory', label: 'Summary', hint: 'Availability and problems of the components' }
  ];

  chargePointId = '';
  status: ChargePointConfigurationStatus | null = null;

  components: ComponentEntry[] = [];
  visibleComponents: ComponentEntry[] = [];
  visibleRows: AttributeRow[] = [];
  totalMatches = 0;
  selectedKey = '';
  search = '';
  writableOnly = false;

  edits: Record<string, string> = {};
  editCount = 0;
  results: Record<string, RowResult> = {};
  flashed = new Set<string>();

  reportMenuOpen = false;
  reporting: ReportBase | null = null;
  sending = false;
  reading = false;
  rebooting = false;
  rebootNeeded = false;

  private allRows: AttributeRow[] = [];
  private poll?: Subscription;

  constructor(
    private _route: ActivatedRoute,
    private _deviceModel: OcppDeviceModelService,
    private _modal: ActionModalService,
    private _confirm: ConfirmService,
    public access: AccessService
  ) { }

  ngOnInit() {
    this.chargePointId = this._route.snapshot.paramMap.get('chargePointId') ?? '';
    this._deviceModel.status(this.chargePointId).subscribe({
      next: status => {
        this.status = status;
        this.loadVariables(true);
      },
      error: error => this.state = error?.status === 404 ? PageState.NotFound : PageState.Error
    });
    this.poll = interval(10000).pipe(switchMap(() => this._deviceModel.status(this.chargePointId)))
      .subscribe({ next: status => this.status = status, error: () => { } });
  }

  ngOnDestroy() {
    this.poll?.unsubscribe();
  }

  @HostListener('window:beforeunload', ['$event'])
  onBeforeUnload(event: BeforeUnloadEvent) {
    if (this.editCount) event.preventDefault();
  }

  @HostListener('document:click')
  closeMenus() {
    this.reportMenuOpen = false;
  }

  get online(): boolean {
    return !!this.status?.isOnline;
  }

  get canOperate(): boolean {
    return this.access.can('OperateChargePoints');
  }

  get selected(): ComponentEntry | undefined {
    return this.components.find(c => c.key === this.selectedKey);
  }

  // ------------------------------------------------------------ loading

  loadVariables(first = false) {
    this._deviceModel.variables(this.chargePointId).subscribe({
      next: variables => {
        this.build(variables);
        this.state = PageState.Success;
      },
      error: () => { if (first) this.state = PageState.Error; }
    });
  }

  private build(variables: DeviceModelVariable[]) {
    const byComponent = new Map<string, ComponentEntry>();
    this.allRows = [];
    for (const variable of variables) {
      const key = componentKey(variable);
      let entry = byComponent.get(key);
      if (!entry) {
        const detail = [variable.componentInstance, evseLabel(variable)].filter(Boolean).join(' · ');
        byComponent.set(key, entry = { key, name: variable.componentName, detail, rows: [], writableCount: 0 });
      }
      for (const attribute of variable.attributes) {
        const row: AttributeRow = {
          key: attributeKey(variable, attribute.type),
          variable,
          attribute,
          writable: attribute.mutability !== 'ReadOnly',
          search: [variable.componentName, variable.componentInstance, variable.variableName, variable.variableInstance, attribute.value]
            .filter(Boolean).join(' ').toLowerCase(),
          choices: (variable.valuesList ?? '').split(',').map(o => o.trim()).filter(Boolean)
        };
        entry.rows.push(row);
        if (row.writable) entry.writableCount++;
        this.allRows.push(row);
      }
    }
    this.components = [...byComponent.values()].sort((a, b) => a.name.localeCompare(b.name) || a.detail.localeCompare(b.detail));
    if (!this.components.some(c => c.key === this.selectedKey)) this.selectedKey = this.components[0]?.key ?? '';
    // Edits for attributes that are gone are dropped.
    const keys = new Set(this.allRows.map(r => r.key));
    for (const key of Object.keys(this.edits)) if (!keys.has(key)) delete this.edits[key];
    this.editCount = Object.keys(this.edits).length;
    this.refresh();
  }

  // ------------------------------------------------------------ filtering

  select(entry: ComponentEntry) {
    this.selectedKey = entry.key;
    this.search = '';
    this.refresh();
  }

  refresh() {
    const term = this.search.trim().toLowerCase();
    const keep = (row: AttributeRow) => (!this.writableOnly || row.writable) && (!term || row.search.includes(term));

    this.visibleComponents = term || this.writableOnly
      ? this.components.filter(c => c.rows.some(keep))
      : this.components;

    const source = term ? this.allRows : (this.selected?.rows ?? []);
    const matches = source.filter(keep);
    this.totalMatches = matches.length;
    this.visibleRows = matches.slice(0, MAX_ROWS);
  }

  // ------------------------------------------------------------ editing

  valueOf(row: AttributeRow): string {
    return row.key in this.edits ? this.edits[row.key] : (row.attribute.value ?? '');
  }

  setValue(row: AttributeRow, value: string) {
    if (value === (row.attribute.value ?? '')) delete this.edits[row.key];
    else this.edits[row.key] = value;
    delete this.results[row.key];
    this.editCount = Object.keys(this.edits).length;
  }

  revert(row: AttributeRow) {
    this.setValue(row, row.attribute.value ?? '');
  }

  discard() {
    this.edits = {};
    this.editCount = 0;
  }

  isEdited(row: AttributeRow): boolean {
    return row.key in this.edits;
  }

  kind(row: AttributeRow): 'boolean' | 'options' | 'number' | 'text' | 'secret' {
    if (row.attribute.mutability === 'WriteOnly') return 'secret';
    const type = (row.variable.dataType ?? '').toLowerCase();
    if (type === 'boolean') return 'boolean';
    if (type === 'optionlist' && row.variable.valuesList) return 'options';
    if (type === 'integer' || type === 'decimal') return 'number';
    return 'text';
  }

  constraint(row: AttributeRow): string {
    const v = row.variable;
    const parts: string[] = [];
    if (v.minLimit != null || v.maxLimit != null) parts.push(`${v.minLimit ?? '…'} – ${v.maxLimit ?? '…'}`);
    if (v.unit) parts.push(v.unit);
    const type = (v.dataType ?? '').toLowerCase();
    if ((type === 'sequencelist' || type === 'memberlist') && v.valuesList) parts.push('from: ' + v.valuesList);
    return parts.join(' · ');
  }

  // ------------------------------------------------------------ charger operations

  send() {
    if (!this.editCount || this.sending) return;
    const edited = this.allRows.filter(r => r.key in this.edits);
    const variables = edited.map(r => this.toVariable(r, this.edits[r.key]));

    this.sending = true;
    this._deviceModel.set(this.chargePointId, variables).subscribe({
      next: result => {
        this.sending = false;
        result.results.forEach((r, i) => {
          const row = edited.find(e => attributeKey(e.variable, e.attribute.type) === attributeKey(r, r.attributeType)) ?? edited[i];
          if (!row) return;
          const tone = r.status === 'Accepted' ? 'ok' : r.status === 'RebootRequired' ? 'warn' : 'bad';
          this.results[row.key] = { status: r.status, reason: r.statusReason, tone };
          if (tone !== 'bad') {
            row.attribute.value = r.value;
            delete this.edits[row.key];
            this.flash(row.key);
          }
        });
        this.editCount = Object.keys(this.edits).length;
        if (result.rebootRequired) this.rebootNeeded = true;
        const title = result.failedCount ? 'Partly applied' : 'Applied';
        const text = `${result.acceptedCount} accepted` + (result.failedCount ? `, ${result.failedCount} rejected (see the rows marked in red)` : '') +
          (result.rebootRequired ? '. A reboot is needed for some of them.' : '.');
        this._modal.popup(result.failedCount ? ActionModalStatusEnum.Error : ActionModalStatusEnum.Success, title, text, 4000);
      },
      error: error => {
        this.sending = false;
        this._modal.popup(ActionModalStatusEnum.Error, 'Not sent', this.httpMessage(error), 5000);
      }
    });
  }

  /** GetVariables for what is on screen, capped so one click stays one or two messages. */
  readLive() {
    const rows = this.visibleRows.filter(r => r.attribute.mutability !== 'WriteOnly').slice(0, 100);
    if (!rows.length || this.reading) return;
    this.reading = true;
    this._deviceModel.get(this.chargePointId, rows.map(r => this.toVariable(r, ''))).subscribe({
      next: results => {
        this.reading = false;
        let changed = 0;
        results.forEach((r, i) => {
          const row = rows.find(e => attributeKey(e.variable, e.attribute.type) === attributeKey(r, r.attributeType)) ?? rows[i];
          if (!row || r.status !== 'Accepted') return;
          if ((row.attribute.value ?? '') !== r.value) changed++;
          row.attribute.value = r.value;
          this.flash(row.key);
        });
        const failed = results.filter(r => r.status !== 'Accepted').length;
        this._modal.popup(ActionModalStatusEnum.Success, 'Live values read',
          `${results.length - failed} read, ${changed} changed since the last report` + (failed ? `, ${failed} not available.` : '.'), 3500);
      },
      error: error => {
        this.reading = false;
        this._modal.popup(ActionModalStatusEnum.Error, 'Not read', this.httpMessage(error), 5000);
      }
    });
  }

  requestReport(base: ReportBase, event?: Event) {
    event?.stopPropagation();
    this.reportMenuOpen = false;
    if (this.reporting) return;
    this.reporting = base;
    this._deviceModel.report(this.chargePointId, base).subscribe({
      next: result => {
        this.reporting = null;
        this.loadVariables();
        this.refreshStatus();
        const ok = result.status === 'Completed';
        this._modal.popup(ok ? ActionModalStatusEnum.Success : ActionModalStatusEnum.Error,
          ok ? 'Device model updated' : 'Report ' + result.status,
          ok ? `${result.reportedVariableCount} variables stored.` : (result.message || 'The charger did not send its report.'), 4000);
      },
      error: error => {
        this.reporting = null;
        this._modal.popup(ActionModalStatusEnum.Error, 'Report failed', this.httpMessage(error), 5000);
      }
    });
  }

  toggleReportMenu(event: Event) {
    event.stopPropagation();
    this.reportMenuOpen = !this.reportMenuOpen;
  }

  reboot() {
    this._confirm.requestConfirmation(
      `Reboot ${this.chargePointId}?`,
      'The charger restarts as soon as no charging session is running (Reset OnIdle). It is unavailable for a minute or two.',
      () => {
        this.rebooting = true;
        this._deviceModel.reboot(this.chargePointId, false).subscribe({
          next: result => {
            this.rebooting = false;
            if (result.status !== 'Rejected') this.rebootNeeded = false;
            this._modal.popup(result.status === 'Rejected' ? ActionModalStatusEnum.Error : ActionModalStatusEnum.Success,
              'Reset ' + result.status, result.status === 'Scheduled' ? 'It will restart once the current session ends.' : 'The charger answered the reset request.', 3500);
          },
          error: error => {
            this.rebooting = false;
            this._modal.popup(ActionModalStatusEnum.Error, 'Not rebooted', this.httpMessage(error), 5000);
          }
        });
      },
      { confirmLabel: 'Reboot', tone: 'danger' });
  }

  // ------------------------------------------------------------ helpers

  private refreshStatus() {
    this._deviceModel.status(this.chargePointId).subscribe({ next: s => this.status = s, error: () => { } });
  }

  private toVariable(row: AttributeRow, value: string): ProvisioningVariable {
    const v = row.variable;
    return {
      groupName: 'Device model',
      componentName: v.componentName,
      componentInstance: v.componentInstance || null,
      evseId: v.evseId ?? null,
      connectorId: v.evseId != null ? v.connectorId ?? null : null,
      variableName: v.variableName,
      variableInstance: v.variableInstance || null,
      attributeType: row.attribute.type,
      value
    };
  }

  private flash(key: string) {
    this.flashed.add(key);
    setTimeout(() => this.flashed.delete(key), 1600);
  }

  private httpMessage(error: any): string {
    if (error?.status === 409) return 'The charge point is not connected.';
    if (error?.status === 403) return 'You need the Operate Charge Points permission for this.';
    return error?.error?.error || error?.error?.message || 'The charge point did not answer.';
  }

  trackRow = (_: number, row: AttributeRow) => row.key;
  trackComponent = (_: number, c: ComponentEntry) => c.key;
}
