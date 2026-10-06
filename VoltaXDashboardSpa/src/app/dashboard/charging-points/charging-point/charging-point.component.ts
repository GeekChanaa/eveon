import { Component, ElementRef, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { FormControl, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { ChargePointService } from 'src/_services/charge-point.service';
import { EnumMappingService } from 'src/_services/enum-mapping.service';
import { HttpClient } from '@angular/common/http';
import { ChargePointBrandService } from 'src/_services/charge-point-brand.service';
import { ChargePointModelService } from 'src/_services/charge-point-model.service';
import { environment } from 'src/environments/environment';
import { PageState } from 'src/_models/_enums/page-state.enum';
import { ConfirmService } from 'src/_services/confirm.service';
import { AccessService } from 'src/_services/access.service';

interface PointField {
  key: string;
  label: string;
  type?: 'text' | 'number' | 'select' | 'textarea';
  optionsKey?: 'brands' | 'models';
  displayKey?: string;
  optional?: boolean;
  enumName?: string;
  readonly?: boolean;
}

@Component({
  selector: 'app-charging-point',
  templateUrl: './charging-point.component.html',
  styleUrls: ['./charging-point.component.sass']
})
export class ChargingPointComponent implements OnInit, OnDestroy {
  @ViewChild('fieldInput') set fieldInput(input: ElementRef<HTMLInputElement> | undefined) {
    if (input) input.nativeElement.focus();
  }
  PageState = PageState;
  state = PageState.Loading;
  chargePointID = 0;
  chargePoint: any = {};
  selectedTab = 'information';
  visitedTabs = new Set(['information']);
  editingField: PointField | null = null;
  draft = new FormControl<any>('');
  saving = false;
  saveError = '';
  feedback = '';
  brands: { value: number; label: string }[] = [];
  models: { value: number; label: string }[] = [];
  optionsError = false;
  newPassword = '';
  passwordCopied = false;
  passwordSaving = false;
  passwordError = '';
  settingCustomPassword = false;
  customPassword = new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(16), Validators.maxLength(40), Validators.pattern(/^[\x20-\x7E]*$/)] });
  qrLoaded = false;
  qrError = false;
  downloading = false;
  downloadError = '';
  regenerating = false;
  regenerateError = '';
  regenerateFeedback = '';
  qrCodeImage = '';
  private qrEndpoint = '';
  private qrLoading = false;
  tabs = [
    { id: 'information', label: 'Information', description: 'Hardware & configuration', icon: 'M12 11v6M12 7v1M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0' },
    { id: 'connectors', label: 'Connectors', description: 'Ports & pricing settings', icon: 'M8 3v5m8-5v5M6 8h12v4a6 6 0 0 1-12 0V8Zm6 10v4' },
    { id: 'qr-code', label: 'QR code', description: 'Preview & download', icon: 'M3 3h6v6H3V3Zm12 0h6v6h-6V3ZM3 15h6v6H3v-6Zm12 0h3v3h3v3h-6v-6ZM3 12h6m3-9v6m0 3h9m-9 3v6' },
    { id: 'events', label: 'Events', description: 'Alarms & monitors', icon: 'M12 9v4m0 4h.01M10.3 3.9 2.4 18a2 2 0 0 0 1.7 3h15.8a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z' },
    { id: 'display-messages', label: 'Display', description: 'Messages on screen', icon: 'M3 5h18v11H3V5Zm5 15h8m-4-4v4' },
    { id: 'customer-information', label: 'Customer data', description: 'Held by the charger', icon: 'M16 20v-1a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v1m6-9a4 4 0 1 0 0-8 4 4 0 0 0 0 8Zm10 9v-1a4 4 0 0 0-3-3.9M16 3.1a4 4 0 0 1 0 7.8' },
    { id: 'logs', label: 'Logs', description: 'Uploaded log files', icon: 'M14 3H6a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9l-6-6Zm0 0v6h6M8 13h8M8 17h5' },
    { id: 'certificates', label: 'Certificates', description: 'Security profile & TLS', icon: 'M12 3 4 6v6c0 4.4 3.4 8.4 8 9 4.6-.6 8-4.6 8-9V6l-8-3Zm-3 9 2 2 4-4' }
  ];
  readonly sections: { title: string; description: string; fields: PointField[] }[] = [
    { title: 'Hardware details', description: 'Identify the charge point and its equipment.', fields: [
      { key: 'chargePointId', label: 'Charge point ID', readonly: true },
      { key: 'chargingStationName', label: 'Charging station', readonly: true },
      { key: 'serialNumber', label: 'Serial number', type: 'text' },
      { key: 'chargePointBrandID', label: 'Brand', type: 'select', optionsKey: 'brands', displayKey: 'make' },
      { key: 'chargePointModelID', label: 'Model', type: 'select', optionsKey: 'models', displayKey: 'modelName' }
    ] },
    { title: 'Configuration', description: 'Manage the operating settings and connection credentials.', fields: [
      { key: 'status', label: 'Status', type: 'select', enumName: 'ChargePointStatus' },
      { key: 'category', label: 'Category', type: 'select', enumName: 'ChargePointCategory' },
      { key: 'username', label: 'Username', readonly: true },
      { key: 'comment', label: 'Comment', type: 'textarea', optional: true }
    ] }
  ];

  constructor(private pointService: ChargePointService, private route: ActivatedRoute, private enums: EnumMappingService,
    private brandService: ChargePointBrandService, private modelService: ChargePointModelService, private http: HttpClient,
    private confirmService: ConfirmService, public access: AccessService) {}

  ngOnInit(): void {
    // Log files are admin-only on the API; customer data needs ViewUsers.
    this.tabs = this.tabs.filter(t => (t.id !== 'logs' || this.access.isAdmin) && (t.id !== 'customer-information' || this.access.can('ViewUsers')));
    this.chargePointID = Number(this.route.snapshot.paramMap.get('id'));
    this.qrEndpoint = environment.apiUrl + "/api/chargepoint/GenerateQrCodeForChargePoint/" + this.chargePointID;
    this.loadOptions();
    this.pointService.getChargePointByID(this.chargePointID).subscribe({
      next: station => { this.chargePoint = station; this.state = PageState.Success; },
      error: error => this.state = error.status === 404 ? PageState.NotFound : PageState.Error
    });
  }

  changeTab(id: string): void {
    if (id === 'qr-code' && !this.qrCodeImage && !this.qrLoading) this.retryQrCode();
    this.selectedTab = id;
    this.visitedTabs.add(id);
  }

  onTabKey(event: KeyboardEvent, index: number): void {
    let next = index;
    if (event.key === 'ArrowRight') next = (index + 1) % this.tabs.length;
    else if (event.key === 'ArrowLeft') next = (index + this.tabs.length - 1) % this.tabs.length;
    else if (event.key === 'Home') next = 0;
    else if (event.key === 'End') next = this.tabs.length - 1;
    else return;
    event.preventDefault();
    this.changeTab(this.tabs[next].id);
    const tablist = (event.currentTarget as HTMLElement).parentElement;
    (tablist?.querySelectorAll('button')[next] as HTMLButtonElement)?.focus();
  }

  loadOptions(): void {
    this.optionsError = false;
    this.brandService.getAllChargePointBrands().subscribe({
      next: data => this.brands = data.map((item: any) => ({ value: item.id, label: item.name })),
      error: () => this.optionsError = true
    });
    this.modelService.getAllChargePointModels().subscribe({
      next: data => this.models = data.map((item: any) => ({ value: item.id, label: item.name })),
      error: () => this.optionsError = true
    });
  }

  options(field: PointField): { value: any; label: string }[] {
    return field.optionsKey ? this[field.optionsKey] : Object.values(this.enums.getEnumMapping(field.enumName || '') || {})
      .map(value => ({ value, label: value.replace(/([a-z])([A-Z])/g, '$1 $2') }));
  }

  displayValue(field: PointField): string {
    const value = this.chargePoint[field.key];
    if (field.optionsKey) return this.options(field).find(option => String(option.value) === String(value))?.label
      || this.chargePoint[field.displayKey || ''] || 'Not specified';
    if (value === null || value === undefined || value === '') return 'Not specified';
    const label = field.enumName ? this.enums.getEnumMapping(field.enumName)?.[value] || value : value;
    return field.enumName ? String(label).replace(/([a-z])([A-Z])/g, '$1 $2') : String(label);
  }

  editField(field: PointField): void {
    if (this.editingField || field.readonly) return;
    this.editingField = field;
    this.feedback = '';
    this.saveError = '';
    const value = this.chargePoint[field.key];
    this.draft.setValidators(field.optional ? [] : field.type === 'number'
      ? [Validators.required, Validators.min(0), Validators.pattern(/^\d+$/)] : [Validators.required]);
    this.draft.reset(field.optionsKey ? this.options(field).find(option => String(option.value) === String(value))?.value ?? null : field.enumName ? this.enums.getEnumMapping(field.enumName)?.[value] || value : value);
  }

  cancelEdit(): void {
    if (this.saving) return;
    const key = this.editingField?.key;
    this.editingField = null;
    this.saveError = '';
    this.restoreEditFocus(key);
  }

  private restoreEditFocus(key?: string): void {
    setTimeout(() => document.getElementById('edit-' + key)?.focus());
  }

  saveField(): void {
    if (!this.editingField || this.saving) return;
    const field = this.editingField;
    const value = typeof this.draft.value === 'string' ? this.draft.value.trim() : this.draft.value;
    this.draft.setValue(value);
    this.draft.markAsTouched();
    if (this.draft.invalid) return;
    const model = { ...this.chargePoint, [field.key]: value };
    if (field.displayKey) model[field.displayKey] = this.options(field).find(option => option.value === value)?.label;
    this.saving = true;
    this.saveError = '';
    this.draft.disable();
    this.pointService.edit(this.chargePointID, model).subscribe({
      next: () => {
        this.chargePoint = model;
        this.saving = false;
        this.draft.enable();
        this.editingField = null;
        this.feedback = field.label + ' updated successfully.';
        this.restoreEditFocus(field.key);
      },
      error: () => {
        this.saving = false;
        this.draft.enable();
        this.saveError = 'Could not save this change. Please try again. Your previous value is unchanged.';
      }
    });
  }
  retryQrCode(): void {
    if (this.qrLoading) return;
    this.qrLoading = true; this.qrError = false; this.qrLoaded = false;
    this.http.get(this.qrEndpoint, { responseType: 'blob' }).subscribe({
      next: blob => { if (this.qrCodeImage) URL.revokeObjectURL(this.qrCodeImage); this.qrCodeImage = URL.createObjectURL(blob); this.qrLoading = false; },
      error: () => { this.qrError = true; this.qrLoading = false; }
    });
  }
  requestRegenerateQrCode(): void {
    if (this.regenerating) return;
    this.confirmService.requestConfirmation(
      'Regenerate QR code?',
      `A new QR code will be generated for ${this.chargePoint.chargePointId}. Previously printed codes will stop working and must be replaced.`,
      () => this.regenerateQrCode(),
      { confirmLabel: 'Regenerate', tone: 'danger' }
    );
  }

  private regenerateQrCode(): void {
    if (this.regenerating) return;
    this.regenerating = true; this.regenerateError = ''; this.regenerateFeedback = '';
    this.pointService.regenerateQrCode(this.chargePointID).subscribe({
      next: () => { this.regenerating = false; this.regenerateFeedback = 'QR code regenerated. Download and print the new code.'; this.retryQrCode(); },
      error: () => { this.regenerating = false; this.regenerateError = 'Could not regenerate the QR code. The current code is unchanged.'; }
    });
  }

  requestGeneratePassword(): void {
    if (this.passwordSaving) return;
    this.confirmService.requestConfirmation(
      this.chargePoint.hasPassword ? 'Regenerate charger password?' : 'Generate charger password?',
      `A new random password will be set for ${this.chargePoint.chargePointId}. The charger must be reconfigured with it before it reconnects, otherwise its connection will be refused.`,
      () => this.savePassword({ generate: true }),
      { confirmLabel: 'Generate', tone: 'danger' }
    );
  }

  startCustomPassword(): void {
    this.settingCustomPassword = true;
    this.customPassword.reset('');
    this.passwordError = '';
  }

  saveCustomPassword(): void {
    this.customPassword.markAsTouched();
    if (this.customPassword.invalid) return;
    this.savePassword({ generate: false, password: this.customPassword.value });
  }

  private savePassword(body: { generate: boolean; password?: string }): void {
    if (this.passwordSaving) return;
    this.passwordSaving = true; this.passwordError = ''; this.newPassword = ''; this.passwordCopied = false;
    this.pointService.setPassword(this.chargePointID, body).subscribe({
      next: result => {
        this.passwordSaving = false;
        this.settingCustomPassword = false;
        this.customPassword.reset('');
        this.newPassword = result.password;
        this.chargePoint = { ...this.chargePoint, hasPassword: true, username: result.username };
      },
      error: error => {
        this.passwordSaving = false;
        this.passwordError = error?.error?.message || 'Could not set the password. The current password is unchanged.';
      }
    });
  }

  copyPassword(): void {
    navigator.clipboard.writeText(this.newPassword).then(() => this.passwordCopied = true, () => this.passwordCopied = false);
  }

  ngOnDestroy(): void { this.newPassword = ''; if (this.qrCodeImage) URL.revokeObjectURL(this.qrCodeImage); }

  downloadQrCode(): void {
    if (this.downloading) return;
    this.downloading = true;
    this.downloadError = '';
    this.http.get(this.qrEndpoint, { responseType: 'blob' }).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `chargepoint_${this.chargePointID}_qr.png`;
        link.click();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
        this.downloading = false;
      },
      error: () => { this.downloading = false; this.downloadError = 'Could not download the QR code. Please try again.'; }
    });
  }

}
