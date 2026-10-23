/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { OcppTransactionService } from './ocpp-transaction.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: OcppTransaction', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [OcppTransactionService]
    });
  });

  it('should ...', inject([OcppTransactionService], (service: OcppTransactionService) => {
    expect(service).toBeTruthy();
  }));
});
