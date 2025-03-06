import { Component, OnInit } from '@angular/core';
import { UserRole } from 'src/_models/_enums/user-role';
import { User } from 'src/_models/user';
import { AuthService } from 'src/_services/auth.service';
import { UserService } from 'src/_services/user.service';

@Component({
  selector: 'app-complete-profile-popup',
  templateUrl: './complete-profile-popup.component.html',
  styleUrls: ['./complete-profile-popup.component.css']
})
export class CompleteProfilePopupComponent implements OnInit {

  completeProfileStep : number = 1;

  userID : number = 0;

  phone : string = "";

  user : User = {
    id: 0,
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    isEmailVerified: false,
    isPhoneVerified: false,
    role: "Admin",
    imageUrl: ''
  }

  emailVerificationCode : string = "";
  phoneVerificationCode : string = "";
  userCar : string = "";
  userGender : string = "";
  userCity : string = "";
  userBirthday : string = "";

  constructor(
    private _authService: AuthService,
    private _userService : UserService
  ) { }

  ngOnInit() {
    this.userID = parseInt(this._authService.getAuthInformation().nameid);
    this._userService.getById(this.userID).subscribe((user) => {
      this.user = user;
      if(user.isEmailVerified){
        this.completeProfileStep = 3;
        if(user.isPhoneVerified){
          this.completeProfileStep = 5;
        }
      }
    })
  }

  // next step
  nextStep(){
    console.log(this.completeProfileStep);
    this.completeProfileStep = this.completeProfileStep+1;
    console.log(this.completeProfileStep);
  }

  // Send Email verification Code
  sendEmailVerificationCode(){
    console.log("sendded");
    this._authService.sendEmailVerificationCode(this.userID).subscribe((data) => {
      this.nextStep();
    });
  }

  // Verifying Email Code
  verifyEmail(){
    this._authService.verifyEmail({email : this.user.email , token : this.emailVerificationCode}).subscribe((data) => {
      this.nextStep();
    });
  }

  // Send Phone verification Code
  sendPhoneVerificationCode(){
    this._authService.sendPhoneVerificationSms({email : this.user.email , phone : this.phone}).subscribe((data) => {
      this.nextStep();
    });
  }

  // Verifying Phone Code
  verifyPhone(){
    // this._authService.verifyPhone({token : this.phoneVerificationCode, phone : this.phone}).subscribe((data) => {
    //   this.nextStep();
    // });
    this.nextStep();
  }

  // Completing additional data
  completeAdditionalData(){
    this.user.birthday = this.userBirthday;
    this.user.car = this.userCar;
    this.user.city = this.userCity;
    this.user.gender = this.userGender;
    
    this.nextStep();
  }
}
