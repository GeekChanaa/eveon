/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { FileManagementService } from './file-management.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: FileManagement', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [FileManagementService]
    });
  });

  it('should ...', inject([FileManagementService], (service: FileManagementService) => {
    expect(service).toBeTruthy();
  }));
});
