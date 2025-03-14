/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { ValidationMessagesService } from './validation-messages.service';

describe('Service: ValidationMessages', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ValidationMessagesService]
    });
  });

  it('should ...', inject([ValidationMessagesService], (service: ValidationMessagesService) => {
    expect(service).toBeTruthy();
  }));
});
