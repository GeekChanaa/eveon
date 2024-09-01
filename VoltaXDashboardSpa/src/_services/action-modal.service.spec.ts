/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { ActionModalService } from './action-modal.service';

describe('Service: ActionModal', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ActionModalService]
    });
  });

  it('should ...', inject([ActionModalService], (service: ActionModalService) => {
    expect(service).toBeTruthy();
  }));
});
