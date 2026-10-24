/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { OCPPConfigurationItemService } from './ocpp-configuration-item.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: OcppConfigurationItem', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [OCPPConfigurationItemService]
    });
  });

  it('should ...', inject([OCPPConfigurationItemService], (service: OCPPConfigurationItemService) => {
    expect(service).toBeTruthy();
  }));
});
