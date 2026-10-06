import { Component, HostListener, OnInit } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { PageState } from 'src/_models/_enums/page-state.enum';
import { ATTRIBUTE_TYPES, OcppDefaultVariable, variableLabel } from 'src/_models/ocpp-provisioning';
import { AccessService } from 'src/_services/access.service';
import { ActionModalService } from 'src/_services/action-modal.service';
import { ConfirmService } from 'src/_services/confirm.service';
import { OcppDefaultConfigurationService } from 'src/_services/ocpp-default-configuration.service';

interface ProfileRow extends OcppDefaultVariable {
  // Local key so new rows (id 0) can be tracked and edited.
  key: number;
}

interface ProfileGroup {
  name: string;
  rows: ProfileRow[];
}

/**
 * Edits the default OCPP 2.0.1 settings profile that is applied automatically to a charge point
 * the first time it connects.
 */
@Component({
  selector: 'app-ocpp-configuration',
  templateUrl: './ocpp-configuration.component.html',
  styleUrls: ['./ocpp-configuration.component.sass']
})
export class OcppConfigurationComponent implements OnInit {

  PageState = PageState;
  state: PageState = PageState.Loading;
  rows: ProfileRow[] = [];
  search = '';
  editingKey: number | null = null;
  isSaving = false;
  errorMessage = '';

  readonly attributeTypes = ATTRIBUTE_TYPES;
  readonly label = variableLabel;

  private savedSnapshot = '';
  private nextKey = 1;
  private cachedGroups: ProfileGroup[] = [];
  private groupsKey: string | null = null;
  private cachedGroupNames: string[] = [];

  constructor(
    private _profileService: OcppDefaultConfigurationService,
    private _access: AccessService,
    private _modalService: ActionModalService,
    private _confirm: ConfirmService
  ) { }

  ngOnInit() {
    this.load();
  }

  get canEdit(): boolean {
    return this._access.can('EditChargePointConfiguration');
  }

  get isDirty(): boolean {
    return this.snapshot() !== this.savedSnapshot;
  }

  get enabledCount(): number {
    return this.rows.filter(r => r.enabled).length;
  }

  // Both lists are bound to *ngFor, so they must keep the same array between change-detection
  // passes. Rebuilding them every pass recreated every row and its ngModel inputs, whose async
  // writes started a new pass: an endless change-detection loop that froze the page.
  get groupNames(): string[] {
    const names = [...new Set(this.rows.map(r => r.groupName || 'General'))];
    if (names.join('\n') !== this.cachedGroupNames.join('\n')) this.cachedGroupNames = names;
    return this.cachedGroupNames;
  }

  get groups(): ProfileGroup[] {
    const term = this.search.trim().toLowerCase();
    const key = [term, this.editingKey, ...this.rows.map(r => `${r.key}\u0001${r.groupName || 'General'}\u0001${term ? this.matches(r, term) : ''}`)].join('\u0002');
    if (key === this.groupsKey) return this.cachedGroups;

    const groups: ProfileGroup[] = [];
    for (const row of this.rows) {
      if (term && row.key !== this.editingKey && !this.matches(row, term)) continue;
      const name = row.groupName || 'General';
      let group = groups.find(g => g.name === name);
      if (!group) groups.push(group = { name, rows: [] });
      group.rows.push(row);
    }
    this.groupsKey = key;
    this.cachedGroups = groups;
    return groups;
  }

  @HostListener('window:beforeunload', ['$event'])
  onBeforeUnload(event: BeforeUnloadEvent) {
    if (this.isDirty) event.preventDefault();
  }

  load() {
    this.state = PageState.Loading;
    this._profileService.getProfile().subscribe({
      next: profile => this.setRows(profile),
      error: () => this.state = PageState.Error
    });
  }

  addRow() {
    const row: ProfileRow = {
      key: this.nextKey++, id: 0, enabled: true, sortOrder: 0,
      groupName: this.groupNames[0] ?? 'General',
      componentName: '', componentInstance: '', evseId: null, connectorId: null,
      variableName: '', variableInstance: '', attributeType: 'Actual', value: '', description: ''
    };
    this.rows = [...this.rows, row];
    this.editingKey = row.key;
    this.search = '';
  }

  edit(row: ProfileRow) {
    this.editingKey = this.editingKey === row.key ? null : row.key;
  }

  remove(row: ProfileRow) {
    this.rows = this.rows.filter(r => r !== row);
    if (this.editingKey === row.key) this.editingKey = null;
  }

  move(row: ProfileRow, direction: -1 | 1) {
    // Moves within its group, which is how the profile is displayed and sent.
    const siblings = this.rows.filter(r => (r.groupName || 'General') === (row.groupName || 'General'));
    const target = siblings[siblings.indexOf(row) + direction];
    if (!target) return;
    const rows = [...this.rows];
    const a = rows.indexOf(row), b = rows.indexOf(target);
    [rows[a], rows[b]] = [rows[b], rows[a]];
    this.rows = rows;
  }

  discard() {
    this.setRows(JSON.parse(this.savedSnapshot));
  }

  save() {
    const invalid = this.rows.find(r => !r.componentName?.trim() || !r.variableName?.trim());
    if (invalid) {
      this.editingKey = invalid.key;
      this.errorMessage = 'Every setting needs a component and a variable name.';
      return;
    }

    this.errorMessage = '';
    this.isSaving = true;
    this._profileService.saveProfile(this.payload()).subscribe({
      next: profile => {
        this.isSaving = false;
        this.setRows(profile);
        this._modalService.popup(ActionModalStatusEnum.Success, 'Saved', 'The default configuration was saved.', 2500);
      },
      error: error => {
        this.isSaving = false;
        this.errorMessage = error?.error?.error || 'The configuration could not be saved.';
      }
    });
  }

  resetToDefaults() {
    this._confirm.requestConfirmation(
      'Reset to the built-in profile?',
      'Every change made to the default configuration will be lost. Charge points that are already configured are not affected.',
      () => {
        this.isSaving = true;
        this._profileService.resetProfile().subscribe({
          next: profile => {
            this.isSaving = false;
            this.setRows(profile);
            this._modalService.popup(ActionModalStatusEnum.Success, 'Reset', 'The built-in profile was restored.', 2500);
          },
          error: () => {
            this.isSaving = false;
            this._modalService.popup(ActionModalStatusEnum.Error, 'Error', 'The profile could not be reset.', 4000);
          }
        });
      },
      { confirmLabel: 'Reset profile', tone: 'danger' });
  }

  trackRow = (_: number, row: ProfileRow) => row.key;
  trackGroup = (_: number, group: ProfileGroup) => group.name;

  private setRows(profile: OcppDefaultVariable[]) {
    this.rows = profile.map(v => ({ ...v, key: this.nextKey++ }));
    this.savedSnapshot = this.snapshot();
    this.editingKey = null;
    this.state = PageState.Success;
  }

  private payload(): OcppDefaultVariable[] {
    return this.rows.map((r, i) => ({
      id: r.id,
      enabled: r.enabled,
      sortOrder: (i + 1) * 10,
      groupName: r.groupName?.trim() || 'General',
      componentName: r.componentName.trim(),
      componentInstance: r.componentInstance?.trim() || null,
      evseId: r.evseId ?? null,
      connectorId: r.evseId != null ? r.connectorId ?? null : null,
      variableName: r.variableName.trim(),
      variableInstance: r.variableInstance?.trim() || null,
      attributeType: r.attributeType,
      value: (r.value ?? '').toString(),
      description: r.description?.trim() || null
    }));
  }

  private snapshot(): string {
    return JSON.stringify(this.payload().map(({ sortOrder, ...rest }) => rest));
  }

  private matches(row: ProfileRow, term: string): boolean {
    return [this.label(row), row.value, row.description, row.groupName]
      .some(text => (text ?? '').toString().toLowerCase().includes(term));
  }
}
