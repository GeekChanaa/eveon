/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { OcppEvDriverService } from './ocpp-ev-driver.service';

describe('Service: OcppEvDriver', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [OcppEvDriverService]
    });
  });

  it('should ...', inject([OcppEvDriverService], (service: OcppEvDriverService) => {
    expect(service).toBeTruthy();
  }));
});
