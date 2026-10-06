import { of, Subject } from 'rxjs';
import { ChargePointEditConnectorComponent } from './charge-point-edit-connector.component';

describe('Connector editor', () => {
  let component: ChargePointEditConnectorComponent;
  let service: any;
  let response: Subject<any>;
  beforeEach(() => {
    response = new Subject();
    service = { getById: () => of({ id: 1, connectorID: 1, evseID: 0, speed: 0, flatFee: 0, pricePerKWh: 0, pricePerMinute: 0, pricePerIdleMinute: 0, pricePerHour: 0, costPerKwh: 0 }), edit: jasmine.createSpy('edit').and.returnValue(response) };
    component = new ChargePointEditConnectorComponent({} as any, { popup: () => {} } as any, service);
    component.connectorID = 1;
    component.ngOnInit();
  });
  it('preserves zero prices and includes the idle-minute price in saves', () => {
    expect(component.connectorForm.valid).toBeTrue();
    expect(component.getControl('flatFee').value).toBe(0);
    component.getControl('pricePerIdleMinute').setValue(2);
    component.cpfOnSubmit();
    expect(service.edit).toHaveBeenCalledWith(1, jasmine.objectContaining({ pricePerIdleMinute: 2, flatFee: 0 }));
  });
  it('prevents duplicate saves and retains the form after a failure', () => {
    component.cpfOnSubmit();
    component.cpfOnSubmit();
    expect(service.edit).toHaveBeenCalledTimes(1);
    response.error(new Error('Network'));
    expect(component.connectorForm.enabled).toBeTrue();
    expect(component.saveError).toBeTruthy();
  });
});
