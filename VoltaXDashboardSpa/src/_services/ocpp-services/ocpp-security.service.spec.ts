/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { OcppSecurityService } from './ocpp-security.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: OcppSecurity', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [OcppSecurityService]
    });
  });

  it('should ...', inject([OcppSecurityService], (service: OcppSecurityService) => {
    expect(service).toBeTruthy();
  }));
});
