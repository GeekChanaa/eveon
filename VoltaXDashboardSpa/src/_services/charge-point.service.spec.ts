/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { ChargePointService } from './charge-point.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: ChargePoint', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [ChargePointService]
    });
  });

  it('should ...', inject([ChargePointService], (service: ChargePointService) => {
    expect(service).toBeTruthy();
  }));
});
