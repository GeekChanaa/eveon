/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { ActionModalService } from './action-modal.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: ActionModal', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [ActionModalService]
    });
  });

  it('should ...', inject([ActionModalService], (service: ActionModalService) => {
    expect(service).toBeTruthy();
  }));
});
