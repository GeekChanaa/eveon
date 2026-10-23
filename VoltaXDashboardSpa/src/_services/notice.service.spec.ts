/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { NoticeService } from './notice.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: Notice', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [NoticeService]
    });
  });

  it('should ...', inject([NoticeService], (service: NoticeService) => {
    expect(service).toBeTruthy();
  }));
});
