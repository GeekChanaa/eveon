/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { ConnectorStatusService } from './connector-status.service';

describe('Service: ConnectorStatus', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ConnectorStatusService]
    });
  });

  it('should ...', inject([ConnectorStatusService], (service: ConnectorStatusService) => {
    expect(service).toBeTruthy();
  }));
});
