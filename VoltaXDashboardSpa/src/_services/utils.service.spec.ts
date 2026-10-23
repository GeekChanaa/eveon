/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { UtilsService } from './utils.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: Utils', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [UtilsService]
    });
  });

  it('should ...', inject([UtilsService], (service: UtilsService) => {
    expect(service).toBeTruthy();
  }));
});
