/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { OcppLocalListItemService } from './ocpp-local-list-item.service';

describe('Service: OcppLocalListItem', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [OcppLocalListItemService]
    });
  });

  it('should ...', inject([OcppLocalListItemService], (service: OcppLocalListItemService) => {
    expect(service).toBeTruthy();
  }));
});
