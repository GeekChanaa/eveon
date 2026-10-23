/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { ElectricVehicleModelService } from './electric-vehicle-model.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: ElectricVehicleModel', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [ElectricVehicleModelService]
    });
  });

  it('should ...', inject([ElectricVehicleModelService], (service: ElectricVehicleModelService) => {
    expect(service).toBeTruthy();
  }));
});
