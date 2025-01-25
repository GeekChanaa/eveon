/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { OcppTransactionService } from './ocpp-transaction.service';

describe('Service: OcppTransaction', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [OcppTransactionService]
    });
  });

  it('should ...', inject([OcppTransactionService], (service: OcppTransactionService) => {
    expect(service).toBeTruthy();
  }));
});
