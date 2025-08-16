import { Component, OnInit } from '@angular/core';
import { PageState } from 'src/_models/_enums/page-state.enum';
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

  PageState = PageState;
  state: PageState = PageState.Loading;
  
  tabsEnum : ProfilePageTabsEnum = ProfilePageTabsEnum.AccountInformationsTab;
  ProfilePageTabsEnum = ProfilePageTabsEnum;
  
  user : any = {};
  userID : number = 0;
  userLoaded : boolean = false;

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
    this.state = PageState.Loading;
    this._userService.getUserInformations(id).subscribe((data) => {
      this.state = PageState.Success;
      this.user = data;
      this._userService.setAvatarUrl(this.user.imageUrl);
      this.userLoaded = true;
    })
  }

  changeTab(tab : any){
    this.tabsEnum = tab;
  }
  
}
