/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { ConnectorStatusService } from './connector-status.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: ConnectorStatus', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [ConnectorStatusService]
    });
  });

  it('should ...', inject([ConnectorStatusService], (service: ConnectorStatusService) => {
    expect(service).toBeTruthy();
  }));
});
