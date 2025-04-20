import { Component, OnInit } from '@angular/core';
import { AuthService } from 'src/_services/auth.service';
import { UserService } from 'src/_services/user.service';


enum ProfilePageTabsEnum {
  AccountInformationsTab = "AccountInformationsTab",
  SecurityTab = "SecurityTab",
  RechargeCardsTab = "RechargeCardsTab",
  PaymentCardsTab = "PaymentCardsTab",
  NotificationSettingsTab = "NotificationSettingsTab"
}

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.sass']
})
export class ProfileComponent implements OnInit {

  tabsEnum : ProfilePageTabsEnum = ProfilePageTabsEnum.AccountInformationsTab;
  
  user : any = {};
  userID : number = 0;

  constructor(
    private _userService : UserService,
    private _authService : AuthService
  ){
  }


  ngOnInit() {
    this.getAuthUserInfos();
  }

  getAuthUserInfos(){
    var decodedToken = this._authService.getAuthInformation();
    this.userID = parseInt(decodedToken.nameid);
    this.getUserByID(this.userID);
  }
  
  getUserByID(id : number ){
    this._userService.getUserInformations(id).subscribe((data) => {
      this.user = data;
      this._userService.setAvatarUrl(this.user.imageUrl);
    })
  }

  changeTab(tab : any){
    this.tabsEnum = tab;
  }
  
}
