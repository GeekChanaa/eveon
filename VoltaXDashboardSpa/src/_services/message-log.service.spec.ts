/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { MessageLogService } from './message-log.service';

describe('Service: MessageLog', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [MessageLogService]
    });
  });

  it('should ...', inject([MessageLogService], (service: MessageLogService) => {
    expect(service).toBeTruthy();
  }));
});
