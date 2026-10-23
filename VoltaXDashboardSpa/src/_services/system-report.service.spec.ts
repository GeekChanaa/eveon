/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { SystemReportService } from './system-report.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: SystemReport', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [SystemReportService]
    });
  });

  it('should ...', inject([SystemReportService], (service: SystemReportService) => {
    expect(service).toBeTruthy();
  }));
});
