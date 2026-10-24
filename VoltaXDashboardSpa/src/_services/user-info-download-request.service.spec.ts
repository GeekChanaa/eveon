/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { UserInfoDownloadRequestService } from './user-info-download-request.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: UserInfoDownloadRequest', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [UserInfoDownloadRequestService]
    });
  });

  it('should ...', inject([UserInfoDownloadRequestService], (service: UserInfoDownloadRequestService) => {
    expect(service).toBeTruthy();
  }));
});
