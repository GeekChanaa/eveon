/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { OcppLocalListVersionService } from './ocpp-local-list-version.service';

describe('Service: OcppLocalListVersion', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [OcppLocalListVersionService]
    });
  });

  it('should ...', inject([OcppLocalListVersionService], (service: OcppLocalListVersionService) => {
    expect(service).toBeTruthy();
  }));
});
