/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { ChargingStationImageService } from './charging-station-image.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: ChargingStationImage', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [ChargingStationImageService]
    });
  });

  it('should ...', inject([ChargingStationImageService], (service: ChargingStationImageService) => {
    expect(service).toBeTruthy();
  }));
});
