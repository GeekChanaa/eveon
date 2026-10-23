/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { ChargePointBrandService } from './charge-point-brand.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: ChargePointBrand', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [ChargePointBrandService]
    });
  });

  it('should ...', inject([ChargePointBrandService], (service: ChargePointBrandService) => {
    expect(service).toBeTruthy();
  }));
});
