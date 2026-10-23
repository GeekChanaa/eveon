/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { InvoiceService } from './invoice.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: Invoice', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [InvoiceService]
    });
  });

  it('should ...', inject([InvoiceService], (service: InvoiceService) => {
    expect(service).toBeTruthy();
  }));
});
