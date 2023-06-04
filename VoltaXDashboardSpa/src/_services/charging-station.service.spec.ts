/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { ChargingStationService } from './charging-station.service';

describe('Service: ChargingStation', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ChargingStationService]
    });
  });

  it('should ...', inject([ChargingStationService], (service: ChargingStationService) => {
    expect(service).toBeTruthy();
  }));
});
