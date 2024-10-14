/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { OcppConfigurationService } from './ocpp-configuration.service';

describe('Service: OcppConfiguration', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [OcppConfigurationService]
    });
  });

  it('should ...', inject([OcppConfigurationService], (service: OcppConfigurationService) => {
    expect(service).toBeTruthy();
  }));
});
