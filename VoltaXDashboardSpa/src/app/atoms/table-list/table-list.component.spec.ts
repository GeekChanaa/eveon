import { HttpErrorResponse, HttpHeaders, HttpResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';
import { PaginatedResult } from 'src/_models/pagination';
import { AccessService } from 'src/_services/access.service';
import { ConfirmService } from 'src/_services/confirm.service';
import { ToastService } from 'src/_services/toast.service';
import { configureVxTest } from 'src/testing/vx-testing.module';
import { TableListComponent } from './table-list.component';
import { FilterChipsComponent } from '../filter-chips/filter-chips.component';
import { ListFilterPanelComponent } from '../list-filter-panel/list-filter-panel.component';

describe('TableListComponent', () => {
  let component: TableListComponent;
  let fixture: ComponentFixture<TableListComponent>;
  let toast: jasmine.SpyObj<ToastService>;
  let getItems: jasmine.Spy;
  const page = (rows: any[], totalPages = 1): PaginatedResult<any[]> =>
    ({ result: rows, pagination: { currentPage: 1, itemsPerPage: 20, totalItems: rows.length, totalPages } } as any);

  beforeEach(async () => {
    Object.keys(sessionStorage).filter(k => k.startsWith('vx.list-filters.')).forEach(k => sessionStorage.removeItem(k));
    toast = jasmine.createSpyObj<ToastService>('ToastService', ['success', 'error', 'apiError']);
    await configureVxTest([TableListComponent, ListFilterPanelComponent], {
      imports: [FilterChipsComponent],
      providers: [
        { provide: ToastService, useValue: toast },
        { provide: AccessService, useValue: { isAdmin: true, can: () => true, canUrl: () => true } }
      ]
    });
    fixture = TestBed.createComponent(TableListComponent);
    component = fixture.componentInstance;
    component.fields = ['name', 'createdAt', 'isActive'];
    component.routeName = 'notices';
    getItems = jasmine.createSpy('getItems').and.returnValue(of(page([{ id: 1, name: 'First', createdAt: '2026-10-20T09:00:00', isActive: true }])));
    component.getItemsObservable = getItems;
  });

  const el = () => fixture.nativeElement as HTMLElement;

  it('loads the first page and shows the rows', () => {
    fixture.detectChanges();
    expect(getItems).toHaveBeenCalledWith(1, 20, {});
    expect(el().querySelectorAll('.row__row').length).toBe(2);
    expect(el().textContent).toContain('First');
    expect(el().textContent).toContain('common.state.yes');
  });

  it('shows an error with a retry instead of an empty list when loading fails', () => {
    getItems.and.returnValue(throwError(() => new HttpErrorResponse({ status: 500 })));
    fixture.detectChanges();
    expect(component.isLoading).toBeFalse();
    expect(el().querySelector('.table-list__error')?.textContent).toContain('operator.table.loadError');
    expect(el().querySelector('app-empty-card')).toBeNull();

    getItems.and.returnValue(of(page([])));
    (el().querySelector('.table-list__error button') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(el().querySelector('.table-list__error')).toBeNull();
    expect(el().querySelector('app-empty-card')).not.toBeNull();
  });

  it('does not show the empty state while loading', () => {
    const pending = new Subject<PaginatedResult<any[]>>();
    getItems.and.returnValue(pending);
    fixture.detectChanges();
    expect(el().querySelector('app-empty-card')).toBeNull();
    expect(el().querySelector('app-progress-bar')).not.toBeNull();
  });

  it('asks before deleting and reports the result', () => {
    const confirm = TestBed.inject(ConfirmService);
    let confirmNow: (() => void) | undefined;
    spyOn(confirm, 'requestConfirmation').and.callFake((_t, _m, onConfirm) => confirmNow = onConfirm);
    const deleteItem = jasmine.createSpy('delete').and.returnValue(of({}));
    component.deleteItemObservable = deleteItem;
    fixture.detectChanges();

    (el().querySelector('.delete-btn') as HTMLButtonElement).click();
    expect(confirm.requestConfirmation).toHaveBeenCalled();
    expect(deleteItem).not.toHaveBeenCalled();
    confirmNow!();
    expect(deleteItem).toHaveBeenCalledWith(1);
    expect(toast.success).toHaveBeenCalledWith('operator.table.deleted');
    expect(getItems).toHaveBeenCalledTimes(2);

    deleteItem.and.returnValue(throwError(() => new HttpErrorResponse({ status: 409, error: { error: 'In use' } })));
    component.delete(1);
    expect(toast.apiError).toHaveBeenCalled();
  });

  it('searches from the first page with the search attributes', () => {
    component.searchByAttributes = ['Name'];
    fixture.detectChanges();
    component.currentPage = 3;
    component.searchValue = 'abc';
    component.search();
    expect(getItems).toHaveBeenCalledWith(1, 20, jasmine.objectContaining({ SearchBy: ['Name'], SearchValue: 'abc' }));
  });

  it('exports the rows of the current search and filters', () => {
    const exportFn = jasmine.createSpy('export').and.returnValue(of(new HttpResponse({ body: new Blob(['a;b']), headers: new HttpHeaders() })));
    component.exportObservable = exportFn;
    spyOn(URL, 'createObjectURL').and.returnValue('blob:x');
    spyOn(HTMLAnchorElement.prototype, 'click');
    fixture.detectChanges();
    component.itemParams = { SearchBy: ['Name'], SearchValue: 'abc' };
    (Array.from(el().querySelectorAll('.list-tool')).find(b => b.textContent?.includes('common.actions.exportCsv')) as HTMLButtonElement).click();
    expect(exportFn).toHaveBeenCalledWith({ SearchBy: ['Name'], SearchValue: 'abc' });
    expect(component.exporting).toBeFalse();
  });

  it('labels columns from the translation prefix', () => {
    component.fieldLabelPrefix = 'operator.notices.fields';
    expect(component.fieldLabel('createdAt')).toBe('Created At');
  });

  describe('filters', () => {
    const defs = [
      { key: 'status', label: 'Status', type: 'multiselect' as const, options: [{ value: 'Active', label: 'Active' }, { value: 'Blocked', label: 'Blocked' }] },
      { key: 'name', label: 'Name', type: 'text' as const },
    ];

    beforeEach(() => {
      component.filterDefs = defs;
      component.searchByAttributes = ['Name'];
    });

    const lastParams = () => getItems.calls.mostRecent().args[2];
    const chips = () => Array.from(el().querySelectorAll('.fc__chip'));

    it('sends the applied filters with their operators, combined with AND, from the first page', () => {
      fixture.detectChanges();
      component.currentPage = 4;
      component.onFiltersApplied({ status: ['Active', 'Blocked'], name: ' ann ' });
      expect(getItems).toHaveBeenCalledWith(1, 20, jasmine.objectContaining({
        FilterBy: ['Status', 'Name'], FilterOperator: ['in', 'contains'], FilterValue: ['Active|Blocked', 'ann'], FilterMethod: '&&'
      }));
    });

    it('shows each applied filter and the search as a chip that removes only itself', () => {
      fixture.detectChanges();
      component.searchValue = 'abc';
      component.search();
      component.onFiltersApplied({ status: ['Active'], name: 'ann' });
      fixture.detectChanges();
      expect(chips().length).toBe(3);

      (chips()[1].querySelector('.fc__remove') as HTMLButtonElement).click();
      fixture.detectChanges();
      expect(chips().length).toBe(2);
      expect(lastParams()).toEqual(jasmine.objectContaining({ FilterBy: ['Name'], SearchValue: 'abc' }));

      (chips()[0].querySelector('.fc__remove') as HTMLButtonElement).click();
      fixture.detectChanges();
      expect(component.searchValue).toBe('');
      expect(lastParams().SearchValue).toBeUndefined();
    });

    it('clears every filter at once', () => {
      fixture.detectChanges();
      component.onFiltersApplied({ status: ['Active'], name: 'ann' });
      fixture.detectChanges();
      (el().querySelector('.fc__clear') as HTMLButtonElement).click();
      fixture.detectChanges();
      expect(chips().length).toBe(0);
      expect(lastParams().FilterBy).toBeUndefined();
    });

    it('remembers the filters of the list for the session', () => {
      fixture.detectChanges();
      component.onFiltersApplied({ status: ['Blocked'] });

      const again = TestBed.createComponent(TableListComponent).componentInstance;
      again.routeName = 'notices';
      again.filterDefs = defs;
      again.fields = ['name'];
      again.getItemsObservable = getItems;
      again.ngOnInit();
      expect(lastParams()).toEqual(jasmine.objectContaining({ FilterBy: ['Status'], FilterValue: ['Blocked'] }));
    });
  });
});
