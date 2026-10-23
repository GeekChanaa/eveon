/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { WebSocketStatusService } from './websocket-status.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: WebsocketStatus', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [WebSocketStatusService]
    });
  });

  it('should ...', inject([WebSocketStatusService], (service: WebSocketStatusService) => {
    expect(service).toBeTruthy();
  }));
});
