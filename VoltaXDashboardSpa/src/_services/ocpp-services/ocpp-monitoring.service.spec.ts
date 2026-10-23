/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { OcppMonitoringService } from './ocpp-monitoring.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: OcppMonitoring', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [OcppMonitoringService]
    });
  });

  it('should ...', inject([OcppMonitoringService], (service: OcppMonitoringService) => {
    expect(service).toBeTruthy();
  }));
});
