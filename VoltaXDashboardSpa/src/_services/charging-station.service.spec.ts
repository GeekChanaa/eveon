/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { ChargingStationService } from './charging-station.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: ChargingStation', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [ChargingStationService]
    });
  });

  it('should ...', inject([ChargingStationService], (service: ChargingStationService) => {
    expect(service).toBeTruthy();
  }));
});
