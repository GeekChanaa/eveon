import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { VerifyPhoneDto } from 'src/_models/_dtos/verify-phone-dto';
import { AuthService } from 'src/_services/auth.service';

@Component({
  selector: 'app-verify-phone',
  templateUrl: './verify-phone.component.html',
  styleUrls: ['./verify-phone.component.sass']
})
export class VerifyPhoneComponent implements OnInit {

  // verification code
  code : string = "" ;
  email : string = "";
  isLoading : boolean = false;

  // phone verification success
  phoneVerifiedSuccess : boolean = false;

  constructor(
    private _route : ActivatedRoute,
    private _authService : AuthService,
    private _router : Router
  ) { }

  ngOnInit() {
    this.email = this._authService.getAuthInformation().unique_name;
  }

  // verify
  verify(){
    this.isLoading = true;
    const token = this.code;
    let verifyPhoneDto : VerifyPhoneDto = {
      token : token,
      email : this.email
    };
    this._authService.verifyPhone(verifyPhoneDto).subscribe((data) => {
      this.isLoading = false;
      this.phoneVerifiedSuccess = true;
      this._router.navigate(['/my-dashboard']);
    }, (error) => {
      this.isLoading = false;
      console.log("this is an error");
      this._router.navigate(['/auth/login'])
    });
  }

}
