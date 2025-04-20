/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { UserInfoDownloadRequestService } from './user-info-download-request.service';

describe('Service: UserInfoDownloadRequest', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [UserInfoDownloadRequestService]
    });
  });

  it('should ...', inject([UserInfoDownloadRequestService], (service: UserInfoDownloadRequestService) => {
    expect(service).toBeTruthy();
  }));
});
