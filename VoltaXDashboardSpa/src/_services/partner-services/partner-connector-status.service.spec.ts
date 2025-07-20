/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { PartnerConnectorStatusService } from './partner-connector-status.service';

describe('Service: PartnerConnectorStatus', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [PartnerConnectorStatusService]
    });
  });

  it('should ...', inject([PartnerConnectorStatusService], (service: PartnerConnectorStatusService) => {
    expect(service).toBeTruthy();
  }));
});
