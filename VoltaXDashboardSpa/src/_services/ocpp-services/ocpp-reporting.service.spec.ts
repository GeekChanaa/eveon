/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { OcppReportingService } from './ocpp-reporting.service';

describe('Service: OcppReporting', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [OcppReportingService]
    });
  });

  it('should ...', inject([OcppReportingService], (service: OcppReportingService) => {
    expect(service).toBeTruthy();
  }));
});
