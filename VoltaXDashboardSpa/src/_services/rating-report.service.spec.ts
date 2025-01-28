/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { RatingReportService } from './rating-report.service';

describe('Service: RatingReport', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [RatingReportService]
    });
  });

  it('should ...', inject([RatingReportService], (service: RatingReportService) => {
    expect(service).toBeTruthy();
  }));
});
