/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { CarChargerService } from './car-charger.service';

describe('Service: CarCharger', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [CarChargerService]
    });
  });

  it('should ...', inject([CarChargerService], (service: CarChargerService) => {
    expect(service).toBeTruthy();
  }));
});
