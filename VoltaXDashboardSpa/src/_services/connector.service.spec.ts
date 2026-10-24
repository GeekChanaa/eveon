/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { ConnectorService } from './connector.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: Connector', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [ConnectorService]
    });
  });

  it('should ...', inject([ConnectorService], (service: ConnectorService) => {
    expect(service).toBeTruthy();
  }));
});
