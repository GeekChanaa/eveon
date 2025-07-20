/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { PartnerChargePointsService } from './partner-charge-points.service';

describe('Service: PartnerChargePoints', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [PartnerChargePointsService]
    });
  });

  it('should ...', inject([PartnerChargePointsService], (service: PartnerChargePointsService) => {
    expect(service).toBeTruthy();
  }));
});
