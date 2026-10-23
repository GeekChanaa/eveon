/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { RequestCallerService } from './request-caller.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: RequestCaller', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [RequestCallerService]
    });
  });

  it('should ...', inject([RequestCallerService], (service: RequestCallerService) => {
    expect(service).toBeTruthy();
  }));
});
