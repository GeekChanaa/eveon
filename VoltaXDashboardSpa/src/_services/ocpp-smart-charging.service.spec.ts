/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { OcppSmartChargingService } from './ocpp-smart-charging.service';

describe('Service: OcppSmartCharging', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [OcppSmartChargingService]
    });
  });

  it('should ...', inject([OcppSmartChargingService], (service: OcppSmartChargingService) => {
    expect(service).toBeTruthy();
  }));
});
