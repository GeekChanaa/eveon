import {AfterViewInit, Component, ElementRef, HostListener, OnInit, Renderer2, ViewChild} from '@angular/core';
import { AuthService } from 'src/_services/auth.service';

enum CompleteProfileEnum{
  EmailVerification = 1,
  PhoneVerification = 2,
  AdditionalInformations = 3
}
@Component({
  selector: 'app-customer-home',
  templateUrl: './customer-home.component.html',
  styleUrls: ['./customer-home.component.sass']
})
export class CustomerHomeComponent implements OnInit {

  CompleteProfileEnum = CompleteProfileEnum;
  currentStep : CompleteProfileEnum = CompleteProfileEnum.EmailVerification;
  user : any = {};
  showProfilePopup : boolean = true;
  verificationCodes : any = {};
  codeSent : any = {};
  profileData : any = {};
  userID : number = 0;
  email : string = "";

  constructor(
    private _authService : AuthService
  ) {}

  getGreeting = () => {
    const hour = new Date().getHours();
    if (hour < 12) return 'Good Morning';
    if (hour < 18) return 'Good Afternoon';
    return 'Good Evening';
  };

  ngOnInit() {
    this.user = this._authService.getAuthInformation();
    this.userID = parseInt(this.user.nameid);
    this.email = this.user.unique_name;
    if(this.user.emailVerified == 'True') this.goNextstep();
  }

  isProfileComplete(){
    return false;
  }

  closePopup(){
    this.showProfilePopup = false;
  }

  goNextstep(){
    if(this.user.phoneVerified == 'False') this.currentStep = CompleteProfileEnum.PhoneVerification;
    else this.currentStep = CompleteProfileEnum.AdditionalInformations;
  }

  saveUserAdditionalInformations(informations : any){
    
  }

 
  verifyPhone(){}
  skipStep(){}
  completeProfile(){}
  sendVerificationSMS(){}

}
