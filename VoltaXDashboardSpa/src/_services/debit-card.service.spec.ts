/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { DebitCardService } from './debit-card.service';

describe('Service: DebitCard', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [DebitCardService]
    });
  });

  it('should ...', inject([DebitCardService], (service: DebitCardService) => {
    expect(service).toBeTruthy();
  }));
});
