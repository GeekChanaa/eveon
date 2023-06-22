/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { FileManagementService } from './file-management.service';

describe('Service: FileManagement', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [FileManagementService]
    });
  });

  it('should ...', inject([FileManagementService], (service: FileManagementService) => {
    expect(service).toBeTruthy();
  }));
});
