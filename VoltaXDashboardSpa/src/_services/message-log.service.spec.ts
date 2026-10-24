/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { MessageLogService } from './message-log.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: MessageLog', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [MessageLogService]
    });
  });

  it('should ...', inject([MessageLogService], (service: MessageLogService) => {
    expect(service).toBeTruthy();
  }));
});
