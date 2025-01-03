/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { SystemReportService } from './system-report.service';

describe('Service: SystemReport', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [SystemReportService]
    });
  });

  it('should ...', inject([SystemReportService], (service: SystemReportService) => {
    expect(service).toBeTruthy();
  }));
});
