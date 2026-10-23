/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { ConfigurationService } from './configuration.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: Configuration', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [ConfigurationService]
    });
  });

  it('should ...', inject([ConfigurationService], (service: ConfigurationService) => {
    expect(service).toBeTruthy();
  }));
});
