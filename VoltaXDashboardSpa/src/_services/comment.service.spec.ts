/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { CommentService } from './comment.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: Comment', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [CommentService]
    });
  });

  it('should ...', inject([CommentService], (service: CommentService) => {
    expect(service).toBeTruthy();
  }));
});
