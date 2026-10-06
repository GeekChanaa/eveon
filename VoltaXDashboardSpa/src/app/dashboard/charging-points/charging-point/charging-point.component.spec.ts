import { Subject, of } from 'rxjs';
import { ChargingPointComponent } from './charging-point.component';
import { EnumMappingService } from 'src/_services/enum-mapping.service';

describe('ChargingPointComponent editing', () => {
  let component: ChargingPointComponent;
  let response: Subject<any>;
  let service: any;
  beforeEach(() => {
    response = new Subject();
    service = { edit: jasmine.createSpy('edit').and.returnValue(response) };
    component = new ChargingPointComponent(service, {} as any, new EnumMappingService(), {} as any, {} as any, {} as any);
    component.chargePointID = 7;
    component.chargePoint = { id: 7, serialNumber: 'Original', status: 0, chargePointBrandID: 2, make: 'Brand A', password: ' secret ', comment: 'Note' };
    component.brands = [{ value: 2, label: 'Brand A' }, { value: 3, label: 'Brand B' }];
  });
  const field = (component: ChargingPointComponent, key: string) => component.sections.flatMap(section => section.fields).find(item => item.key === key)!;

  it('saves the API field key only after a successful response and prevents duplicate requests', () => {
    component.editField(field(component, 'serialNumber'));
    component.draft.setValue('Updated');
    component.saveField();
    component.saveField();
    expect(service.edit).toHaveBeenCalledTimes(1);
    expect(service.edit).toHaveBeenCalledWith(7, jasmine.objectContaining({ serialNumber: 'Updated' }));
    expect(component.chargePoint.serialNumber).toBe('Original');
    response.next({});
    expect(component.chargePoint.serialNumber).toBe('Updated');
  });
  it('selects the current brand by ID and updates its display label on save', () => {
    component.editField(field(component, 'chargePointBrandID'));
    expect(component.draft.value).toBe(2);
    component.draft.setValue(3);
    component.saveField();
    response.next({});
    expect(component.chargePoint.chargePointBrandID).toBe(3);
    expect(component.chargePoint.make).toBe('Brand B');
  });
  it('keeps failed edits and the original value intact', () => {
    component.editField(field(component, 'serialNumber'));
    component.draft.setValue('Draft');
    component.saveField();
    response.error(new Error('Network'));
    expect(component.chargePoint.serialNumber).toBe('Original');
    expect(component.draft.value).toBe('Draft');
    expect(component.draft.enabled).toBeTrue();
    expect(component.saveError).toBeTruthy();
  });
  it('preserves drafts across tabs and cancels without saving', () => {
    component.editField(field(component, 'serialNumber'));
    component.draft.setValue('Draft');
    component.changeTab('connectors');
    component.changeTab('information');
    expect(component.draft.value).toBe('Draft');
    component.cancelEdit();
    expect(service.edit).not.toHaveBeenCalled();
  });
  it('masks passwords and preserves intentional whitespace on save', () => {
    const password = field(component, 'password');
    expect(component.displayValue(password)).not.toContain('secret');
    component.editField(password);
    component.saveField();
    expect(service.edit).toHaveBeenCalledWith(7, jasmine.objectContaining({ password: ' secret ' }));
  });
  it('allows clearing an optional comment and maps enum zero correctly', () => {
    component.editField(field(component, 'status'));
    expect(component.draft.value).toBe('Available');
    component.cancelEdit();
    component.editField(field(component, 'comment'));
    component.draft.setValue('');
    component.saveField();
    expect(service.edit).toHaveBeenCalledWith(7, jasmine.objectContaining({ comment: '' }));
  });
});
