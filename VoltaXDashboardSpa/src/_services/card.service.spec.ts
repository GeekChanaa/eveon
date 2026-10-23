/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { CardService } from './card.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: Card', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [CardService]
    });
  });

  it('should ...', inject([CardService], (service: CardService) => {
    expect(service).toBeTruthy();
  }));
});
