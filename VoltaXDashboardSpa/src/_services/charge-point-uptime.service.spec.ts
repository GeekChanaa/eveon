/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { ChargePointUptimeService } from './charge-point-uptime.service';

describe('Service: ChargePointUptime', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ChargePointUptimeService]
    });
  });

  it('should ...', inject([ChargePointUptimeService], (service: ChargePointUptimeService) => {
    expect(service).toBeTruthy();
  }));
});
