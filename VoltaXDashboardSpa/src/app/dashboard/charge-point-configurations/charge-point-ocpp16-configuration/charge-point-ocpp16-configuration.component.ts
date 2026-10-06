import { Component, Input, OnChanges, OnDestroy } from '@angular/core';
import { Subscription } from 'rxjs';
import { ActionModalService } from 'src/_services/action-modal.service';
import { showOcppCommandFeedback } from 'src/_services/ocpp-services/ocpp-command-feedback';
import { Ocpp16ConfigurationKey, OcppConfigurationService } from 'src/_services/ocpp-services/ocpp-configuration.service';

/**
 * OCPP 1.6 configuration keys (GetConfiguration / ChangeConfiguration).
 * Renders nothing unless the charger is currently connected with OCPP 1.6.
 */
@Component({
  selector: 'app-charge-point-ocpp16-configuration',
  templateUrl: './charge-point-ocpp16-configuration.component.html',
  styleUrls: ['./charge-point-ocpp16-configuration.component.sass']
})
export class ChargePointOcpp16ConfigurationComponent implements OnChanges, OnDestroy {

  /** Charger identity (ChargePoint.chargePointId), not the DB id. */
  @Input() chargePointId = '';
  @Input() canOperate = false;

  protocolVersion: string | null = null;

  keysInput = '';
  loading = false;
  loaded = false;
  keys: Ocpp16ConfigurationKey[] = [];
  unknownKeys: string[] = [];

  editingKey: string | null = null;
  draft = '';
  saving = false;
  lastStatus: Record<string, string> = {};

  private sub?: Subscription;

  constructor(
    private _configurationService: OcppConfigurationService,
    private _modalService: ActionModalService
  ) { }

  get isOcpp16(): boolean {
    return this.protocolVersion === 'ocpp1.6';
  }

  ngOnChanges() {
    this.sub?.unsubscribe();
    this.protocolVersion = null;
    if (!this.chargePointId) return;
    this.sub = this._configurationService.getProtocolVersion(this.chargePointId).subscribe({
      next: result => this.protocolVersion = result?.protocolVersion ?? null,
      error: () => this.protocolVersion = null
    });
  }

  ngOnDestroy() {
    this.sub?.unsubscribe();
  }

  getConfiguration() {
    if (this.loading) return;
    const keys = this.keysInput.split(',').map(k => k.trim()).filter(Boolean);
    this.loading = true;
    this._configurationService.getConfiguration(this.chargePointId, keys).subscribe({
      next: data => {
        this.loading = false;
        this.loaded = true;
        this.keys = [...(data?.response?.configurationKey ?? [])].sort((a, b) => a.key.localeCompare(b.key));
        this.unknownKeys = data?.response?.unknownKey ?? [];
        this.lastStatus = {};
        this.cancelEdit();
        showOcppCommandFeedback(this._modalService, data);
      },
      error: error => {
        this.loading = false;
        showOcppCommandFeedback(this._modalService, error);
      }
    });
  }

  edit(row: Ocpp16ConfigurationKey) {
    if (row.readonly || !this.canOperate || this.saving) return;
    this.editingKey = row.key;
    this.draft = row.value ?? '';
  }

  cancelEdit() {
    this.editingKey = null;
    this.draft = '';
  }

  save(row: Ocpp16ConfigurationKey) {
    if (this.saving) return;
    const value = this.draft;
    this.saving = true;
    this._configurationService.changeConfiguration(this.chargePointId, row.key, value).subscribe({
      next: data => {
        this.saving = false;
        if (data?.status) this.lastStatus[row.key] = data.status;
        if (showOcppCommandFeedback(this._modalService, data)) {
          row.value = value;
          this.cancelEdit();
        }
      },
      error: error => {
        this.saving = false;
        showOcppCommandFeedback(this._modalService, error);
      }
    });
  }

  statusTone(status: string): string {
    if (status === 'Accepted') return 'cfg-badge_ok';
    if (status === 'RebootRequired') return 'cfg-badge_warn';
    return 'cfg-badge_bad';
  }

  trackKey = (_: number, row: Ocpp16ConfigurationKey) => row.key;
}
