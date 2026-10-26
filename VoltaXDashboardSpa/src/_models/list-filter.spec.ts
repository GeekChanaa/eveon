import { ListFilterDef, activeFilterCount, dayBoundary, toFilterParams, withoutFilter } from './list-filter';

describe('list filters', () => {
  const defs: ListFilterDef[] = [
    { key: 'status', label: 'Status', type: 'select' },
    { key: 'type', label: 'Type', type: 'multiselect' },
    { key: 'name', label: 'Name', type: 'text', match: 'startswith' },
    { key: 'createdAt', label: 'Created', type: 'date-range' },
    { key: 'balance', label: 'Balance', type: 'number-range' },
    { key: 'verified', label: 'Verified', type: 'boolean', field: 'IsEmailVerified' },
    { key: 'city', label: 'City', type: 'select', field: 'City-Name' },
  ];

  it('ignores empty values', () => {
    const params = toFilterParams(defs, { status: '', type: [], name: '  ', createdAt: { from: null, to: null }, verified: null });
    expect(params.FilterBy).toEqual([]);
    expect(activeFilterCount({ status: '', type: [], createdAt: { from: null, to: '' } })).toBe(0);
  });

  it('maps each filter type to its operator', () => {
    const params = toFilterParams(defs, {
      status: 'Active', type: ['A', 'B'], name: 'Jo', balance: { from: 10 }, verified: false, city: 'Tanger'
    });
    expect(params.FilterBy).toEqual(['Status', 'Type', 'Name', 'Balance', 'IsEmailVerified', 'City-Name']);
    expect(params.FilterOperator).toEqual(['eq', 'in', 'startswith', 'gte', 'eq', 'eq']);
    expect(params.FilterValue).toEqual(['Active', 'A|B', 'Jo', '10', 'false', 'Tanger']);
    expect(params.FilterMethod).toBe('&&');
  });

  it('turns a date range into whole local days', () => {
    const params = toFilterParams(defs, { createdAt: { from: '2026-10-01', to: '2026-10-15' } });
    expect(params.FilterOperator).toEqual(['gte', 'lte']);
    expect(params.FilterValue).toEqual([dayBoundary('2026-10-01', false), dayBoundary('2026-10-15', true)]);
    expect(new Date(params.FilterValue[0]).getHours()).toBe(0);
    expect(new Date(params.FilterValue[1]).getHours()).toBe(23);
  });

  it('checks presence for a boolean on a field that may be empty', () => {
    const presence: ListFilterDef[] = [{ key: 'suspended', field: 'SuspendedAt', label: 'Suspended', type: 'boolean', presence: true }];
    expect(toFilterParams(presence, { suspended: true }).FilterValue).toEqual(['false']);
    expect(toFilterParams(presence, { suspended: false }).FilterOperator).toEqual(['null']);
    expect(toFilterParams(presence, { suspended: false }).FilterValue).toEqual(['true']);
  });

  it('removes one filter or all of them', () => {
    expect(withoutFilter({ a: 1, b: 2 }, 'a')).toEqual({ b: 2 });
    expect(withoutFilter({ a: 1, b: 2 }, null)).toEqual({});
  });
});
