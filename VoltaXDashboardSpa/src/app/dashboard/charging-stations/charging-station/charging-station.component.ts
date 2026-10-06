import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { FormControl, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { ChargingStationService } from 'src/_services/charging-station.service';
import { EnumMappingService } from 'src/_services/enum-mapping.service';
import { PageState } from 'src/_models/_enums/page-state.enum';

interface StationField {
  key: string;
  label: string;
  type?: 'text' | 'number' | 'select';
  enumName?: string;
  readonly?: boolean;
}

@Component({
  selector: 'app-charging-station',
  templateUrl: './charging-station.component.html',
  styleUrls: ['./charging-station.component.sass']
})
export class ChargingStationComponent implements OnInit {
  @ViewChild('fieldInput') set fieldInput(input: ElementRef<HTMLInputElement> | undefined) {
    if (input) input.nativeElement.focus();
  }
  PageState = PageState;
  state = PageState.Loading;
  chargingStationID = 0;
  chargingStation: any = {};
  selectedTab = 'information';
  visitedTabs = new Set(['information']);
  editingField: StationField | null = null;
  draft = new FormControl<any>('');
  saving = false;
  saveError = '';
  feedback = '';
  readonly tabs = [
    { id: 'information', label: 'Information', description: 'Station details & location', icon: 'M12 11v6M12 7v1M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0' },
    { id: 'charge-points', label: 'Charge points', description: 'Connected equipment', icon: 'M13 2 4 14h7l-1 8 10-12h-7l1-8Z' },
    { id: 'images', label: 'Images', description: 'Station photos & uploads', icon: 'M4 3h16a1 1 0 0 1 1 1v16a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1Zm-1 14 5-5 4 4 4-6 5 7M8 7h.01' }
  ];
  readonly sections: { title: string; description: string; fields: StationField[] }[] = [
    { title: 'Station details', description: 'Manage the identity and operating settings of this station.', fields: [
      { key: 'name', label: 'Station name', type: 'text' },
      { key: 'partnerName', label: 'Partner', readonly: true },
      { key: 'network', label: 'Network', type: 'select', enumName: 'ChargingStationNetworkEnum' },
      { key: 'category', label: 'Category', type: 'select', enumName: 'ChargingStationCategoryEnum' },
      { key: 'chargerQuantity', label: 'Charger quantity', type: 'number' },
      { key: 'parkingType', label: 'Parking type', type: 'select', enumName: 'ParkingTypeEnum' },
      { key: 'status', label: 'Status', type: 'select', enumName: 'ChargingStationStatusEnum' }
    ] },
    { title: 'Location', description: 'Keep the station address accurate and easy to find.', fields: [
      { key: 'address', label: 'Address', type: 'text' },
      { key: 'city', label: 'City', type: 'text' }
    ] }
  ];

  constructor(private stationService: ChargingStationService, private route: ActivatedRoute, private enums: EnumMappingService) {}

  ngOnInit(): void {
    this.chargingStationID = Number(this.route.snapshot.paramMap.get('id'));
    this.stationService.getChargingStationByID(this.chargingStationID).subscribe({
      next: station => { this.chargingStation = station; this.state = PageState.Success; },
      error: error => this.state = error.status === 404 ? PageState.NotFound : PageState.Error
    });
  }

  changeTab(id: string): void {
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

  options(field: StationField): string[] {
    return Object.values(this.enums.getEnumMapping(field.enumName || '') || {});
  }

  displayValue(field: StationField): string {
    const value = this.chargingStation[field.key];
    if (value === null || value === undefined || value === '') return 'Not specified';
    const label = field.enumName ? this.enums.getEnumMapping(field.enumName)?.[value] || value : value;
    return field.enumName ? String(label).replace(/([a-z])([A-Z])/g, '$1 $2') : String(label);
  }

  editField(field: StationField): void {
    if (this.editingField || field.readonly) return;
    this.editingField = field;
    this.feedback = '';
    this.saveError = '';
    const value = this.chargingStation[field.key];
    this.draft.setValidators(field.type === 'number'
      ? [Validators.required, Validators.min(0), Validators.pattern(/^\d+$/)] : [Validators.required]);
    this.draft.reset(field.enumName ? this.enums.getEnumMapping(field.enumName)?.[value] || value : value);
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
    const model = { ...this.chargingStation, [field.key]: value };
    this.saving = true;
    this.saveError = '';
    this.draft.disable();
    this.stationService.edit(this.chargingStationID, model).subscribe({
      next: () => {
        this.chargingStation = model;
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
}
