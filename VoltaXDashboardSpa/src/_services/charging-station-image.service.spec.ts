/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { ChargingStationImageService } from './charging-station-image.service';

describe('Service: ChargingStationImage', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ChargingStationImageService]
    });
  });

  it('should ...', inject([ChargingStationImageService], (service: ChargingStationImageService) => {
    expect(service).toBeTruthy();
  }));
});
