/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { OcppConfigurationItemService } from './ocpp-configuration-item.service';

describe('Service: OcppConfigurationItem', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [OcppConfigurationItemService]
    });
  });

  it('should ...', inject([OcppConfigurationItemService], (service: OcppConfigurationItemService) => {
    expect(service).toBeTruthy();
  }));
});
