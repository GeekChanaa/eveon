/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { ChargePointUptimeService } from './charge-point-uptime.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: ChargePointUptime', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [ChargePointUptimeService]
    });
  });

  it('should ...', inject([ChargePointUptimeService], (service: ChargePointUptimeService) => {
    expect(service).toBeTruthy();
  }));
});
