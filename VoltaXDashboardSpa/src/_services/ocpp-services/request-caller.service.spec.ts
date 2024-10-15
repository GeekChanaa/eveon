/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { RequestCallerService } from './request-caller.service';

describe('Service: RequestCaller', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [RequestCallerService]
    });
  });

  it('should ...', inject([RequestCallerService], (service: RequestCallerService) => {
    expect(service).toBeTruthy();
  }));
});
