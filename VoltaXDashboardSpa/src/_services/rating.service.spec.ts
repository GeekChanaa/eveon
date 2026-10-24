/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { RatingService } from './rating.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: Rating', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [RatingService]
    });
  });

  it('should ...', inject([RatingService], (service: RatingService) => {
    expect(service).toBeTruthy();
  }));
});
