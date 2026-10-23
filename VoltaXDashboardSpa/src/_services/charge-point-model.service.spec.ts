/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { ChargePointModelService } from './charge-point-model.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: ChargePointModel', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [ChargePointModelService]
    });
  });

  it('should ...', inject([ChargePointModelService], (service: ChargePointModelService) => {
    expect(service).toBeTruthy();
  }));
});
