import { AccessService } from 'src/_services/access.service';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { PageState } from 'src/_models/_enums/page-state.enum';
import { AuthService } from 'src/_services/auth.service';
import { UserService } from 'src/_services/user.service';


enum ProfilePageTabsEnum {
  AccountInformationsTab = "AccountInformationsTab",
  SecurityTab = "SecurityTab",
  RechargeCardsTab = "RechargeCardsTab",
  PaymentCardsTab = "PaymentCardsTab",
  NotificationSettingsTab = "NotificationSettingsTab",
  PrivacyTab = "PrivacyTab"
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
    private access: AccessService,
    private _userService : UserService,
    private _authService : AuthService,
    private _route : ActivatedRoute
  ){
  }


  ngOnInit() {
    // Deep link from the data export email: /my-dashboard/profile?tab=privacy&export=...
    if (this._route.snapshot.queryParamMap.get('tab') === 'privacy') this.changeTab(ProfilePageTabsEnum.PrivacyTab);
    // Forced 2FA enrollment (Auth:RequireTwoFactorForAdmins) lands here
    if (this._route.snapshot.queryParamMap.get('tab') === 'security') this.changeTab(ProfilePageTabsEnum.SecurityTab);
    this.getAuthUserInfos();
  }

  getAuthUserInfos(){

    var decodedToken = this._authService.getAuthInformation();
    this.userID = parseInt(decodedToken.nameid);
    this.getUserByID(this.userID);
  }
  
  getUserByID(id : number ){
    this.state = PageState.Loading;
    this.access.profile().subscribe((data) => {
      this.state = PageState.Success;
      this.user = data;
      this._userService.setAvatarUrl(this.user.imageUrl);
      this.userLoaded = true;
    })
  }

  visitedTabs = new Set(["AccountInformationsTab"]);
  changeTab(tab : any){
    this.visitedTabs.add(tab);
    this.tabsEnum = tab;
  }
  
}
