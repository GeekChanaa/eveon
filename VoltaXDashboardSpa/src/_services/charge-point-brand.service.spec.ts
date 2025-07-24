/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { ChargePointBrandService } from './charge-point-brand.service';

describe('Service: ChargePointBrand', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ChargePointBrandService]
    });
  });

  it('should ...', inject([ChargePointBrandService], (service: ChargePointBrandService) => {
    expect(service).toBeTruthy();
  }));
});
