/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { OcppComponentsService } from './ocpp-components.service';

describe('Service: OcppComponents', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [OcppComponentsService]
    });
  });

  it('should ...', inject([OcppComponentsService], (service: OcppComponentsService) => {
    expect(service).toBeTruthy();
  }));
});
