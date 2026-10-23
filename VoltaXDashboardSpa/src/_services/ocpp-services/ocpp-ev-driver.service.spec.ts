/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { OcppEvDriverService } from './ocpp-ev-driver.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: OcppEvDriver', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [OcppEvDriverService]
    });
  });

  it('should ...', inject([OcppEvDriverService], (service: OcppEvDriverService) => {
    expect(service).toBeTruthy();
  }));
});
