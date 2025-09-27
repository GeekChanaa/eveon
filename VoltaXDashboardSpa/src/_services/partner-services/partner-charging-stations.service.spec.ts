/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { PartnerChargingStationsService } from './partner-charging-stations.service';

describe('Service: PartnerChargingStations', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [PartnerChargingStationsService]
    });
  });

  it('should ...', inject([PartnerChargingStationsService], (service: PartnerChargingStationsService) => {
    expect(service).toBeTruthy();
  }));
});
