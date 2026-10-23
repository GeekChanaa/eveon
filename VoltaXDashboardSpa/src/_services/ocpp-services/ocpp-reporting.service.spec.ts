/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { OcppReportingService } from './ocpp-reporting.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: OcppReporting', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [OcppReportingService]
    });
  });

  it('should ...', inject([OcppReportingService], (service: OcppReportingService) => {
    expect(service).toBeTruthy();
  }));
});
