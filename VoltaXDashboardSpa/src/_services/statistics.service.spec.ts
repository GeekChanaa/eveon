/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { StatisticsService } from './statistics.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: Statistics', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [StatisticsService]
    });
  });

  it('should ...', inject([StatisticsService], (service: StatisticsService) => {
    expect(service).toBeTruthy();
  }));
});
