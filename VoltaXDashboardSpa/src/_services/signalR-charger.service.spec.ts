/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { SignalRChargerService } from './signalR-charger.service';

describe('Service: SignalRCharger', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [SignalRChargerService]
    });
  });

  it('should ...', inject([SignalRChargerService], (service: SignalRChargerService) => {
    expect(service).toBeTruthy();
  }));
});
