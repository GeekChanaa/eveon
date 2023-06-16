/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { ChargePointService } from './charge-point.service';

describe('Service: ChargePoint', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ChargePointService]
    });
  });

  it('should ...', inject([ChargePointService], (service: ChargePointService) => {
    expect(service).toBeTruthy();
  }));
});
