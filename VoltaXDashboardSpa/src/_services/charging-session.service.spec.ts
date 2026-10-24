/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { ChargingSessionService } from './charging-session.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: ChargingSession', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [ChargingSessionService]
    });
  });

  it('should ...', inject([ChargingSessionService], (service: ChargingSessionService) => {
    expect(service).toBeTruthy();
  }));
});
