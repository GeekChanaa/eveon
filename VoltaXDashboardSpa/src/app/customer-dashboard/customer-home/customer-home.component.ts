import {AfterViewInit, Component, ElementRef, HostListener, OnInit, Renderer2, ViewChild} from '@angular/core';
import { AuthService } from 'src/_services/auth.service';
@Component({
  selector: 'app-customer-home',
  templateUrl: './customer-home.component.html',
  styleUrls: ['./customer-home.component.sass']
})
export class CustomerHomeComponent implements OnInit {

  currentStep : number = 1;
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
    var user = this._authService.getAuthInformation();
    this.userID = parseInt(user.nameid);
    this.email = user.unique_name;
  }

  isProfileComplete(){
    return false;
  }

  closePopup(){
    this.showProfilePopup = false;
  }

  

 
  verifyPhone(){}
  skipStep(){}
  completeProfile(){}
  sendVerificationSMS(){}

}
