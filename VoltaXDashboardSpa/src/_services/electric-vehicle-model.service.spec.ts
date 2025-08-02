/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { ElectricVehicleModelService } from './electric-vehicle-model.service';

describe('Service: ElectricVehicleModel', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ElectricVehicleModelService]
    });
  });

  it('should ...', inject([ElectricVehicleModelService], (service: ElectricVehicleModelService) => {
    expect(service).toBeTruthy();
  }));
});
