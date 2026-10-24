/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { SystemReportCommentService } from './system-report-comment.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: SystemReportComment', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [SystemReportCommentService]
    });
  });

  it('should ...', inject([SystemReportCommentService], (service: SystemReportCommentService) => {
    expect(service).toBeTruthy();
  }));
});
