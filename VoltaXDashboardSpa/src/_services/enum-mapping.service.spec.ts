/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { EnumMappingService } from './enum-mapping.service';

describe('Service: EnumMapping', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [EnumMappingService]
    });
  });

  it('should ...', inject([EnumMappingService], (service: EnumMappingService) => {
    expect(service).toBeTruthy();
  }));
});
