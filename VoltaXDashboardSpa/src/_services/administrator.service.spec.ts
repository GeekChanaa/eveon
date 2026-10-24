/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { AdministratorService } from './administrator.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: Administrator', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [AdministratorService]
    });
  });

  it('should ...', inject([AdministratorService], (service: AdministratorService) => {
    expect(service).toBeTruthy();
  }));
});
