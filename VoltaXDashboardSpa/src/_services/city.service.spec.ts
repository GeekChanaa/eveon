/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { CityService } from './city.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: City', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [CityService]
    });
  });

  it('should ...', inject([CityService], (service: CityService) => {
    expect(service).toBeTruthy();
  }));
});
