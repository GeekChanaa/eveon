/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { PartnerAuthService } from './partner-auth.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: PartnerAuth', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [PartnerAuthService]
    });
  });

  it('should ...', inject([PartnerAuthService], (service: PartnerAuthService) => {
    expect(service).toBeTruthy();
  }));
});
