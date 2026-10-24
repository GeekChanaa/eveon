/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { OcppComponentsService } from './ocpp-components.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: OcppComponents', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [OcppComponentsService]
    });
  });

  it('should ...', inject([OcppComponentsService], (service: OcppComponentsService) => {
    expect(service).toBeTruthy();
  }));
});
