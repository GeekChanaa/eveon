/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { RatingReportService } from './rating-report.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: RatingReport', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [RatingReportService]
    });
  });

  it('should ...', inject([RatingReportService], (service: RatingReportService) => {
    expect(service).toBeTruthy();
  }));
});
