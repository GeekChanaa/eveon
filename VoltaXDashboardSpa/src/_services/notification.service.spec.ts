/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { NotificationService } from './notification.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: Notification', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [NotificationService]
    });
  });

  it('should ...', inject([NotificationService], (service: NotificationService) => {
    expect(service).toBeTruthy();
  }));
});
