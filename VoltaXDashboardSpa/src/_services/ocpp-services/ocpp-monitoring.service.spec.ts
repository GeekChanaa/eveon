/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { OcppMonitoringService } from './ocpp-monitoring.service';

describe('Service: OcppMonitoring', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [OcppMonitoringService]
    });
  });

  it('should ...', inject([OcppMonitoringService], (service: OcppMonitoringService) => {
    expect(service).toBeTruthy();
  }));
});
