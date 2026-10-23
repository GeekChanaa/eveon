/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { ReportService } from './report.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: Report', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [ReportService]
    });
  });

  it('should ...', inject([ReportService], (service: ReportService) => {
    expect(service).toBeTruthy();
  }));
});
