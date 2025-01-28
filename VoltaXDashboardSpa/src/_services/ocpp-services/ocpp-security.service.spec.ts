/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { OcppSecurityService } from './ocpp-security.service';

describe('Service: OcppSecurity', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [OcppSecurityService]
    });
  });

  it('should ...', inject([OcppSecurityService], (service: OcppSecurityService) => {
    expect(service).toBeTruthy();
  }));
});
