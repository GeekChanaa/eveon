/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { ChargingSessionService } from './charging-session.service';

describe('Service: ChargingSession', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ChargingSessionService]
    });
  });

  it('should ...', inject([ChargingSessionService], (service: ChargingSessionService) => {
    expect(service).toBeTruthy();
  }));
});
