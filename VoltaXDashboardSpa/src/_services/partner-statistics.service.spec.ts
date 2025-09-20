/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { PartnerStatisticsService } from './partner-statistics.service';

describe('Service: PartnerStatistics', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [PartnerStatisticsService]
    });
  });

  it('should ...', inject([PartnerStatisticsService], (service: PartnerStatisticsService) => {
    expect(service).toBeTruthy();
  }));
});
