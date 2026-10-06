import { Subject, of } from 'rxjs';
import { ChargingStationComponent } from './charging-station.component';
import { EnumMappingService } from 'src/_services/enum-mapping.service';

describe('ChargingStationComponent editing', () => {
  let component: ChargingStationComponent;
  let response: Subject<any>;
  let service: any;

  beforeEach(() => {
    response = new Subject();
    service = { edit: jasmine.createSpy('edit').and.returnValue(response), getChargingStationByID: () => of({}) };
    component = new ChargingStationComponent(service, { snapshot: { paramMap: { get: () => '7' } } } as any, new EnumMappingService());
    component.chargingStationID = 7;
    component.chargingStation = { id: 7, name: 'Station', chargerQuantity: 2, parkingType: 'ParallelParking', city: 'Rabat', category: 0 };
  });

  it('uses the API field key and waits for success before changing the displayed value', () => {
    const field = component.sections[0].fields.find(item => item.key === 'chargerQuantity')!;
    component.editField(field);
    component.draft.setValue(4);
    component.saveField();
    expect(service.edit).toHaveBeenCalledWith(7, jasmine.objectContaining({ chargerQuantity: 4 }));
    expect(service.edit.calls.mostRecent().args[1]['Charger Quantity']).toBeUndefined();
    expect(component.chargingStation.chargerQuantity).toBe(2);
    component.saveField();
    expect(service.edit).toHaveBeenCalledTimes(1);
    response.next({});
    expect(component.chargingStation.chargerQuantity).toBe(4);
    expect(component.editingField).toBeNull();
  });

  it('retains the draft and original value when a save fails', () => {
    component.editField(component.sections[1].fields[1]);
    component.draft.setValue('Casablanca');
    component.saveField();
    response.error(new Error('Network error'));
    expect(component.chargingStation.city).toBe('Rabat');
    expect(component.draft.value).toBe('Casablanca');
    expect(component.draft.enabled).toBeTrue();
    expect(component.saveError).toBeTruthy();
  });

  it('preserves an unfinished edit across tabs and cancels without saving', () => {
    component.editField(component.sections[1].fields[1]);
    component.draft.setValue('Casablanca');
    component.changeTab('images');
    component.changeTab('information');
    expect(component.draft.value).toBe('Casablanca');
    component.cancelEdit();
    expect(component.chargingStation.city).toBe('Rabat');
    expect(service.edit).not.toHaveBeenCalled();
  });

  it('rejects fractional quantities and whitespace-only text', () => {
    component.editField(component.sections[0].fields.find(item => item.key === 'chargerQuantity')!);
    component.draft.setValue(1.5);
    component.saveField();
    expect(service.edit).not.toHaveBeenCalled();
    component.cancelEdit();
    component.editField(component.sections[1].fields[1]);
    component.draft.setValue('   ');
    component.saveField();
    expect(service.edit).not.toHaveBeenCalled();
  });

  it('maps a zero-valued enum to its option when editing', () => {
    component.editField(component.sections[0].fields.find(item => item.key === 'category')!);
    expect(component.draft.value).toBe('Public');
  });
});
