/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { ConnectorUptimeService } from './connector-uptime.service';

describe('Service: ConnectorUptime', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ConnectorUptimeService]
    });
  });

  it('should ...', inject([ConnectorUptimeService], (service: ConnectorUptimeService) => {
    expect(service).toBeTruthy();
  }));
});
