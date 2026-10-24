/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { ConnectorUptimeService } from './connector-uptime.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: ConnectorUptime', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [ConnectorUptimeService]
    });
  });

  it('should ...', inject([ConnectorUptimeService], (service: ConnectorUptimeService) => {
    expect(service).toBeTruthy();
  }));
});
