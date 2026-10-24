/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { ConfirmService } from './confirm.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: Confirm', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [ConfirmService]
    });
  });

  it('should ...', inject([ConfirmService], (service: ConfirmService) => {
    expect(service).toBeTruthy();
  }));
});
