/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { WebsocketStatusService } from './websocket-status.service';

describe('Service: WebsocketStatus', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [WebsocketStatusService]
    });
  });

  it('should ...', inject([WebsocketStatusService], (service: WebsocketStatusService) => {
    expect(service).toBeTruthy();
  }));
});
