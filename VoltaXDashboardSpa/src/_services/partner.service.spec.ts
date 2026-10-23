/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { PartnerService } from './partner.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: Partner', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [PartnerService]
    });
  });

  it('should ...', inject([PartnerService], (service: PartnerService) => {
    expect(service).toBeTruthy();
  }));
});
