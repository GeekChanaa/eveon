/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { PartnerAuthService } from './partner-auth.service';

describe('Service: PartnerAuth', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [PartnerAuthService]
    });
  });

  it('should ...', inject([PartnerAuthService], (service: PartnerAuthService) => {
    expect(service).toBeTruthy();
  }));
});
