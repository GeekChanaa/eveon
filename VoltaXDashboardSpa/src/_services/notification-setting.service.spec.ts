/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { NotificationSettingService } from './notification-setting.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: NotificationSetting', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [NotificationSettingService]
    });
  });

  it('should ...', inject([NotificationSettingService], (service: NotificationSettingService) => {
    expect(service).toBeTruthy();
  }));
});
