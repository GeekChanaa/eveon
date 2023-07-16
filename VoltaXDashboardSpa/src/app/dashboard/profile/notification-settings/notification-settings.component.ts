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
    var userID = parseInt(this._authService.getAuthInformation().nameid);
    this._notificationTypeService.getNotificationTypesBy(role).subscribe((data) => {
      this.notificationTypes = data;
      this._notificationSettingService.getUserNotificationSettings(userID).subscribe((nsData) => {
        console.log("mapping notification settings to notification types");
        console.log(this.notificationTypes);
  
        // Assign the enabled status to each notification type
          console.log("nsData");
          console.log(nsData);
          console.log("notificationTypes");
          console.log(this.notificationTypes);
          this.notificationTypes = this.notificationTypes.map((type) => {
            console.log("type id ");
            console.log(type.id);
            let setting = nsData.find(setting => (setting.notificationTypeID) === (type.id));
            console.log("this is the setting");
            console.log(setting);
            type.active = setting ? setting.active : false;
            return type;
        });

        console.log(this.notificationTypes);
      })
    })
  }
  
  saveNotificationSettings(){
    var role = this._authService.getRole();
    var userID = parseInt(this._authService.getAuthInformation().nameid);
  
    this.notificationTypes.forEach((type) => {
      let notificationSetting = {
        email: false,
        user: null,
        userId: userID,
        notificationTypeId: type.id,
        urgent: false,
        active: type.active
      };
      console.log(notificationSetting);
      this._notificationSettingService.saveUserNotificationSetting(notificationSetting).subscribe((data) => {
         console.log("Updated notification setting for type: ", type.id);
      });
    });
  }
  


}
