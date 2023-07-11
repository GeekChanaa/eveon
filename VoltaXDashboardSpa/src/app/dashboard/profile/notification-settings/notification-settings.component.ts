import { Component, OnInit } from '@angular/core';
import { AuthService } from 'src/_services/auth.service';
import { NotificationSettingService } from 'src/_services/notification-setting.service';
import { NotificationTypeService } from 'src/_services/notification-type.service';

@Component({
  selector: 'app-notification-settings',
  templateUrl: './notification-settings.component.html',
  styleUrls: ['./notification-settings.component.css']
})
export class NotificationSettingsComponent implements OnInit {

  notificationTypes : any[] = [];
  constructor(
    private _notificationSettingService : NotificationSettingService,
    private _notificationTypeService : NotificationTypeService,
    private _authService : AuthService
  ) { }

  ngOnInit() {
    this.getAllNotificationTypes();
  }

  // getting all notificationTypes for the current user role
  getAllNotificationTypes(){
    var role = this._authService.getRole();
    this._notificationTypeService.getNotificationTypesBy(role).subscribe((data) => {
      this.notificationTypes = data;
    })
  }


}
