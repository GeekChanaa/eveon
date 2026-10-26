/**
 * Declarative list filters: a page describes what can be filtered, the shared panel edits the values and the
 * chips show (and remove) what is applied. `toFilterParams` turns the values into the API's GlobalParams
 * (FilterBy / FilterValue / FilterOperator, combined with AND).
 */

/** select: one value. multiselect: any of several. text: free text. date / number: one value. *-range: from / to. boolean: yes / no. */
export type ListFilterType = 'select' | 'multiselect' | 'text' | 'date' | 'number' | 'date-range' | 'number-range' | 'boolean';

export interface ListFilterOption {
  value: string | number;
  /** Translation key or plain text. */
  label: string;
}

export interface ListFilterDef {
  /** Key in the values object; also the API field (FilterBy) unless `field` is set. "Nav-Prop" filters on a reference. */
  key: string;
  field?: string;
  /** Translation key or plain text. */
  label: string;
  type: ListFilterType;
  options?: ListFilterOption[];
  /** Text filters: how the text matches (default contains). */
  match?: 'contains' | 'startswith' | 'eq';
  /** Translation key or plain text. */
  placeholder?: string;
  /** Boolean filters: "yes" means the field has a value (e.g. Suspended on SuspendedAt), "no" that it is empty. */
  presence?: boolean;
}

export interface ListFilterRange {
  from?: string | number | null;
  to?: string | number | null;
}

/** Filter values by key: a scalar, a list (multiselect) or a range. Empty values mean "not filtered". */
export type ListFilterValues = Record<string, any>;

export interface ListFilterParams {
  FilterBy: string[];
  FilterValue: string[];
  FilterOperator: string[];
  FilterMethod: '&&';
}

export function isEmptyValue(value: any): boolean {
  if (value == null || (typeof value === 'string' && value.trim() === '')) return true;
  if (Array.isArray(value)) return value.length === 0;
  if (typeof value === 'object') return isEmptyValue(value.from) && isEmptyValue(value.to);
  return false;
}

/** The values that actually filter, without empty entries. */
export function activeFilterValues(values: ListFilterValues | null | undefined): ListFilterValues {
  const active: ListFilterValues = {};
  for (const key in values ?? {}) {
    const value = values![key];
    if (isEmptyValue(value)) continue;
    if (value && typeof value === 'object' && !Array.isArray(value)) {
      const range: ListFilterRange = {};
      if (!isEmptyValue(value.from)) range.from = value.from;
      if (!isEmptyValue(value.to)) range.to = value.to;
      active[key] = range;
    } else {
      active[key] = Array.isArray(value) ? [...value] : value;
    }
  }
  return active;
}

export function activeFilterCount(values: ListFilterValues | null | undefined): number {
  return Object.keys(activeFilterValues(values)).length;
}

/** A local calendar day (yyyy-MM-dd) as the UTC instant of its first or last millisecond. */
export function dayBoundary(day: string, end: boolean): string {
  const [y, m, d] = day.split('-').map(Number);
  const date = end ? new Date(y, m - 1, d, 23, 59, 59, 999) : new Date(y, m - 1, d, 0, 0, 0, 0);
  return date.toISOString();
}

/** The API filter params for these values (AND between filters). */
export function toFilterParams(defs: ListFilterDef[], values: ListFilterValues | null | undefined): ListFilterParams {
  const params: ListFilterParams = { FilterBy: [], FilterValue: [], FilterOperator: [], FilterMethod: '&&' };
  const push = (field: string, op: string, value: any) => {
    params.FilterBy.push(field);
    params.FilterOperator.push(op);
    params.FilterValue.push(String(value));
  };
  const active = activeFilterValues(values);

  for (const def of defs) {
    const value = active[def.key];
    if (value === undefined) continue;
    const field = def.field ?? capitalize(def.key);
    switch (def.type) {
      case 'multiselect':
        push(field, 'in', (value as any[]).join('|'));
        break;
      case 'text':
        push(field, def.match ?? 'contains', String(value).trim());
        break;
      case 'date':
        // One calendar day: from its start to its end.
        push(field, 'gte', dayBoundary(value, false));
        push(field, 'lte', dayBoundary(value, true));
        break;
      case 'date-range':
        if (value.from != null) push(field, 'gte', dayBoundary(value.from, false));
        if (value.to != null) push(field, 'lte', dayBoundary(value.to, true));
        break;
      case 'boolean':
        if (def.presence) push(field, 'null', value ? 'false' : 'true');
        else push(field, 'eq', value);
        break;
      case 'number-range':
        if (value.from != null) push(field, 'gte', value.from);
        if (value.to != null) push(field, 'lte', value.to);
        break;
      default:
        push(field, 'eq', value);
    }
  }
  return params;
}

/** The entries of a page's filter object that differ from its defaults (what the user actually chose). */
export function changedFrom<T extends object>(values: T, defaults: T): ListFilterValues {
  const changed: ListFilterValues = {};
  for (const key of Object.keys(values) as (keyof T & string)[]) {
    if (values[key] !== defaults[key]) changed[key] = values[key];
  }
  return changed;
}

/** A copy of the values without one filter (or without everything when key is null). */
export function withoutFilter(values: ListFilterValues, key: string | null): ListFilterValues {
  if (key == null) return {};
  const copy = { ...values };
  delete copy[key];
  return copy;
}

function capitalize(value: string): string {
  return value.charAt(0).toUpperCase() + value.slice(1);
}
