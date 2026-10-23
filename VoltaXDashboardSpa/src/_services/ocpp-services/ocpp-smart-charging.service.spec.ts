/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { OcppSmartChargingService } from './ocpp-smart-charging.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: OcppSmartCharging', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [OcppSmartChargingService]
    });
  });

  it('should ...', inject([OcppSmartChargingService], (service: OcppSmartChargingService) => {
    expect(service).toBeTruthy();
  }));
});
