/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { PartnerService } from './partner.service';

describe('Service: Partner', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [PartnerService]
    });
  });

  it('should ...', inject([PartnerService], (service: PartnerService) => {
    expect(service).toBeTruthy();
  }));
});
