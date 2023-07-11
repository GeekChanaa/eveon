/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { NotificationSettingService } from './notification-setting.service';

describe('Service: NotificationSetting', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [NotificationSettingService]
    });
  });

  it('should ...', inject([NotificationSettingService], (service: NotificationSettingService) => {
    expect(service).toBeTruthy();
  }));
});
