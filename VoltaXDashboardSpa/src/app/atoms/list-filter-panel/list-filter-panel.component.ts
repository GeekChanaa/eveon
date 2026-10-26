import { Component, ElementRef, EventEmitter, HostListener, Input, Output, ViewChild } from '@angular/core';
import {
  ListFilterDef, ListFilterOption, ListFilterValues, activeFilterCount, activeFilterValues
} from 'src/_models/list-filter';

type DatePreset = 'today' | 'last7' | 'last30' | 'thisMonth';

/**
 * The "Filters" button of a list and its panel. The panel edits a draft of the values; nothing changes until
 * Apply (or Enter). Reset empties the draft. The button shows how many filters are applied.
 */
@Component({
  selector: 'app-list-filter-panel',
  templateUrl: './list-filter-panel.component.html',
  styleUrls: ['./list-filter-panel.component.sass']
})
export class ListFilterPanelComponent {
  @Input() defs: ListFilterDef[] = [];
  /** The applied values. */
  @Input() values: ListFilterValues = {};
  /** Translation key of the panel title. */
  @Input() titleKey = 'operator.table.filtering';
  @Output() apply = new EventEmitter<ListFilterValues>();

  @ViewChild('toggleButton') toggleButton?: ElementRef<HTMLButtonElement>;

  open = false;
  draft: ListFilterValues = {};
  readonly presets: DatePreset[] = ['today', 'last7', 'last30', 'thisMonth'];
  readonly panelId = 'list-filters-' + Math.random().toString(36).slice(2, 8);

  constructor(private _host: ElementRef<HTMLElement>) {}

  get appliedCount(): number {
    return activeFilterCount(this.values);
  }

  get draftCount(): number {
    return activeFilterCount(this.draft);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (this.open && !this._host.nativeElement.contains(event.target as Node)) this.close(false);
  }

  toggle(event: MouseEvent): void {
    event.stopPropagation();
    this.open ? this.close(false) : this.openPanel();
  }

  openPanel(): void {
    // Ranges are edited as objects: every range filter gets one, even when nothing is applied.
    this.draft = activeFilterValues(this.values);
    for (const def of this.defs) {
      if (this.isRange(def) && !this.draft[def.key]) this.draft[def.key] = { from: null, to: null };
      if (def.type === 'multiselect' && !this.draft[def.key]) this.draft[def.key] = [];
    }
    this.open = true;
  }

  close(refocus = true): void {
    this.open = false;
    if (refocus) this.toggleButton?.nativeElement.focus();
  }

  submit(): void {
    this.apply.emit(activeFilterValues(this.draft));
    this.close();
  }

  reset(): void {
    this.draft = {};
    for (const def of this.defs) this.clearOne(def);
  }

  isRange(def: ListFilterDef): boolean {
    return def.type === 'date-range' || def.type === 'number-range';
  }

  isSelected(def: ListFilterDef, option: ListFilterOption): boolean {
    return (this.draft[def.key] ?? []).some((v: any) => String(v) === String(option.value));
  }

  toggleOption(def: ListFilterDef, option: ListFilterOption): void {
    const current: any[] = this.draft[def.key] ?? [];
    this.draft[def.key] = this.isSelected(def, option)
      ? current.filter(v => String(v) !== String(option.value))
      : [...current, option.value];
  }

  setBoolean(def: ListFilterDef, value: boolean | null): void {
    this.draft[def.key] = value;
  }

  clearOne(def: ListFilterDef): void {
    this.draft[def.key] = this.isRange(def) ? { from: null, to: null } : def.type === 'multiselect' ? [] : null;
  }

  hasValue(def: ListFilterDef): boolean {
    return activeFilterValues({ [def.key]: this.draft[def.key] })[def.key] !== undefined;
  }

  applyPreset(def: ListFilterDef, preset: DatePreset): void {
    const today = new Date();
    const day = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
    const back = (days: number) => new Date(today.getFullYear(), today.getMonth(), today.getDate() - days);
    const from = preset === 'today' ? today
      : preset === 'last7' ? back(6)
      : preset === 'last30' ? back(29)
      : new Date(today.getFullYear(), today.getMonth(), 1);
    this.draft[def.key] = { from: day(from), to: day(today) };
  }

  trackByKey = (_: number, def: ListFilterDef) => def.key;
}

function pad(n: number): string {
  return n < 10 ? '0' + n : String(n);
}
