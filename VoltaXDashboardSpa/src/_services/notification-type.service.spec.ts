/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { NotificationTypeService } from './notification-type.service';

describe('Service: NotificationType', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [NotificationTypeService]
    });
  });

  it('should ...', inject([NotificationTypeService], (service: NotificationTypeService) => {
    expect(service).toBeTruthy();
  }));
});
