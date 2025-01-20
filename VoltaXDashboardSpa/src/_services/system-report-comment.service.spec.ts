/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { SystemReportCommentService } from './system-report-comment.service';

describe('Service: SystemReportComment', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [SystemReportCommentService]
    });
  });

  it('should ...', inject([SystemReportCommentService], (service: SystemReportCommentService) => {
    expect(service).toBeTruthy();
  }));
});
