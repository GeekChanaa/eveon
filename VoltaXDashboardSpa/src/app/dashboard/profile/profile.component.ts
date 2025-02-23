import { Component, OnInit } from '@angular/core';
import { User } from 'src/_models/user';
import { AuthService } from 'src/_services/auth.service';
import { FileManagementService } from 'src/_services/file-management.service';
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

  // Editing bools
  editingFirstName : Boolean = false;
  editingLastName : Boolean = false;
  editingEmail : Boolean = false;
  editingPhone : Boolean = false;
  editingPassword : Boolean = false;

  //userid
  userID : number = 0;

  // Profile Picture
  profilePicture : any = {};


  // TabsEnum
  tabsEnum : ProfilePageTabsEnum = ProfilePageTabsEnum.AccountInformationsTab;


  // CurrentUser
  user : User = {
    firstName: '',
    lastName: '',
    id: 0,
    email: '',
    phone: '',
    role: "Customer",
    isEmailVerified: false,
    isPhoneVerified: false
  };

  updateUserObservable = (id : number, model : any) => this._userService.edit(id, model);

  // Constructor
  constructor(
    private _authService : AuthService,
    private _userService : UserService,
    private _fileManagementService : FileManagementService
  ) {    
   }

  // On init cycle hook
  ngOnInit() {
    this.getAuthUserInfos();
  }

  getAuthUserInfos(){
    var decodedToken = this._authService.getAuthInformation();
    var userid = parseInt(decodedToken.nameid);
    this._userService.getById(userid).subscribe((user) => {
      this.user = user;
    });
  }

  changeTab(tab : any){
    this.tabsEnum = tab;
  }

  uploadPicture(files : any){
    const formData = new FormData();
    this.profilePicture = <File>files[0];
    formData.append('imageFile',this.profilePicture, "userX.png");
    this._fileManagementService.uploadProfilePicture(formData).subscribe((data) => {
      console.log("Profile Picture successfully uploaded to the destination");
    });
  }

  getUserByID(id : number ){
    this._userService.getById(id).subscribe((data) => {
      this.user = data;
    })
  }
}
