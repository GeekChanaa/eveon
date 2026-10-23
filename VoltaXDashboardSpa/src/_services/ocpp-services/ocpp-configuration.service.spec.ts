/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { OcppConfigurationService } from './ocpp-configuration.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: OcppConfiguration', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [OcppConfigurationService]
    });
  });

  it('should ...', inject([OcppConfigurationService], (service: OcppConfigurationService) => {
    expect(service).toBeTruthy();
  }));
});
