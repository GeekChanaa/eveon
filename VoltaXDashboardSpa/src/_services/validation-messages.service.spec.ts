/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { ValidationMessagesService } from './validation-messages.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: ValidationMessages', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [ValidationMessagesService]
    });
  });

  it('should ...', inject([ValidationMessagesService], (service: ValidationMessagesService) => {
    expect(service).toBeTruthy();
  }));
});
