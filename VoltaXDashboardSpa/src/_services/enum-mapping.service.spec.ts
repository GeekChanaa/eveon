/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { EnumMappingService } from './enum-mapping.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: EnumMapping', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [EnumMappingService]
    });
  });

  it('should ...', inject([EnumMappingService], (service: EnumMappingService) => {
    expect(service).toBeTruthy();
  }));
});
