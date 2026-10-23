/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { SignalRChargerService } from './signalR-charger.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: SignalRCharger', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [SignalRChargerService]
    });
  });

  it('should ...', inject([SignalRChargerService], (service: SignalRChargerService) => {
    expect(service).toBeTruthy();
  }));
});
