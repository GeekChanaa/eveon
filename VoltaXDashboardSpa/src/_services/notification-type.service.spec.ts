/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { NotificationTypeService } from './notification-type.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: NotificationType', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [NotificationTypeService]
    });
  });

  it('should ...', inject([NotificationTypeService], (service: NotificationTypeService) => {
    expect(service).toBeTruthy();
  }));
});
