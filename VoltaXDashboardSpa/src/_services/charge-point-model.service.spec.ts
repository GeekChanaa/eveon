/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { ChargePointModelService } from './charge-point-model.service';

describe('Service: ChargePointModel', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ChargePointModelService]
    });
  });

  it('should ...', inject([ChargePointModelService], (service: ChargePointModelService) => {
    expect(service).toBeTruthy();
  }));
});
