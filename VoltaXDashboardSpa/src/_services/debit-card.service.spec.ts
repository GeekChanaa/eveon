/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { DebitCardService } from './debit-card.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: DebitCard', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [DebitCardService]
    });
  });

  it('should ...', inject([DebitCardService], (service: DebitCardService) => {
    expect(service).toBeTruthy();
  }));
});
