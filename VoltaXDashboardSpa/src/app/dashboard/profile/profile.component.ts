import { Component, OnInit } from '@angular/core';
import { User } from 'src/_models/user';
import { AuthService } from 'src/_services/auth.service';
import { UserService } from 'src/_services/user.service';
enum ProfilePageTabsEnum {
  AccountInformationsTab = "AccountInformationsTab",
  SecurityTab = "SecurityTab",
  RechargeCardsTab = "RechargeCardsTab",
  PaymentCardsTab = "PaymentCardsTab",
}

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {

  // TabsEnum
  tabsEnum : ProfilePageTabsEnum = ProfilePageTabsEnum.AccountInformationsTab;


  // CurrentUser
  user : User = {
    firstName: '',
    lastName: '',
    id: 0,
    email: '',
    phone: ''  
  };

  // Constructor
  constructor(
    private _authService : AuthService,
    private _userService : UserService
  ) { }

  // On init cycle hook
  ngOnInit() {
    this.getAuthUserInfos();
  }

  // Getting authenticated user informations
  getAuthUserInfos(){
    var decodedToken = this._authService.getAuthInformation();
    var userid = parseInt(decodedToken.nameid);
    this._userService.getById(userid).subscribe((user) => {
      console.log(user);
      this.user = user;
    });
    console.log(this.user);
  }


  // Changing current tab
  changeTab(tab : any){
    this.tabsEnum = tab;
  }

}
