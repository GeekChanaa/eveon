/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { OCPPLocalListVersionService } from './ocpp-local-list-version.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: OcppLocalListVersion', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [OCPPLocalListVersionService]
    });
  });

  it('should ...', inject([OCPPLocalListVersionService], (service: OCPPLocalListVersionService) => {
    expect(service).toBeTruthy();
  }));
});
