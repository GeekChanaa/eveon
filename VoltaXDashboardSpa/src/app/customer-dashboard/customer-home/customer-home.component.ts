import {AfterViewInit, Component, ElementRef, HostListener, OnInit, Renderer2, ViewChild} from '@angular/core';
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

  constructor(
  ) {}

  getGreeting = () => {
    const hour = new Date().getHours();
    if (hour < 12) return 'Good Morning';
    if (hour < 18) return 'Good Afternoon';
    return 'Good Evening';
  };

  ngOnInit() {
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
