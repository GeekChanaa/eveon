import { Component, EventEmitter, Input, OnChanges, Output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule, formatDate } from '@angular/common';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ListFilterDef, ListFilterValues, activeFilterValues } from 'src/_models/list-filter';
import { LanguageService } from 'src/_services/language.service';

export interface FilterChip {
  key: string;
  label: string;
  value: string;
}

/**
 * The applied filters of a list as chips: "Status: Active, Blocked ×". Each chip removes its filter;
 * "Clear all" removes every one. A search text, when given, is shown as a chip too (key '__search').
 */
@Component({
  selector: 'app-filter-chips',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  templateUrl: './filter-chips.component.html',
  styleUrls: ['./filter-chips.component.sass']
})
export class FilterChipsComponent implements OnChanges {
  static readonly SEARCH_KEY = '__search';

  @Input() defs: ListFilterDef[] = [];
  @Input() values: ListFilterValues | null = {};
  @Input() search: string | null = '';
  /** Emits the key of the removed filter ('__search' for the search text). */
  @Output() remove = new EventEmitter<string>();
  @Output() clearAll = new EventEmitter<void>();

  chips: FilterChip[] = [];

  constructor(private _translate: TranslateService, private _language: LanguageService) {
    // Labels and dates follow the interface language.
    this._translate.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => this.chips = this.buildChips());
  }

  ngOnChanges(): void {
    this.chips = this.buildChips();
  }

  private buildChips(): FilterChip[] {
    const chips: FilterChip[] = [];
    const text = (this.search ?? '').trim();
    if (text) chips.push({ key: FilterChipsComponent.SEARCH_KEY, label: this.t('operator.table.search'), value: `“${text}”` });

    const active = activeFilterValues(this.values);
    for (const def of this.defs) {
      const value = active[def.key];
      if (value === undefined) continue;
      chips.push({ key: def.key, label: this.t(def.label), value: this.describe(def, value) });
    }
    return chips;
  }

  private describe(def: ListFilterDef, value: any): string {
    const option = (v: any) => {
      const match = def.options?.find(o => String(o.value) === String(v));
      return match ? this.t(match.label) : String(v);
    };
    switch (def.type) {
      case 'select':
        return option(value);
      case 'multiselect':
        return (value as any[]).map(option).join(', ');
      case 'boolean':
        return this.t(value ? 'common.state.yes' : 'common.state.no');
      case 'date':
        return this.day(value);
      case 'date-range':
        return this.range(value.from != null ? this.day(value.from) : null, value.to != null ? this.day(value.to) : null);
      case 'number-range':
        return this.range(value.from, value.to);
      default:
        return String(value);
    }
  }

  private range(from: any, to: any): string {
    if (from != null && to != null) return `${from} – ${to}`;
    if (from != null) return `≥ ${from}`;
    return `≤ ${to}`;
  }

  private day(value: string): string {
    const [y, m, d] = value.split('-').map(Number);
    return formatDate(new Date(y, m - 1, d), 'mediumDate', this._language.locale);
  }

  private t(key: string): string {
    return key ? this._translate.instant(key) : '';
  }

  trackByKey = (_: number, chip: FilterChip) => chip.key;
}
