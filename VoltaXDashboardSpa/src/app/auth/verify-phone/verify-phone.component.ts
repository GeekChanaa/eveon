import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { VerifyPhoneDto } from 'src/_models/_dtos/verify-phone-dto';
import { AuthService } from 'src/_services/auth.service';

@Component({
  selector: 'app-verify-phone',
  templateUrl: './verify-phone.component.html',
  styleUrls: ['./verify-phone.component.css']
})
export class VerifyPhoneComponent implements OnInit {

  // verification code
  code : string = "" ;

  // phone verification success
  phoneVerifiedSuccess : boolean = false;

  constructor(
    private _route : ActivatedRoute,
    private _authService : AuthService,
    private _router : Router
  ) { }

  ngOnInit() {
  }

  // verify
  verify(){
    this._route.queryParams.subscribe((params) => {
      const token = this.code;
      const phone = params['phone'];
      let verifyPhoneDto : VerifyPhoneDto = {
        token : token,
        phone : phone
      };
      this._authService.verifyPhone(verifyPhoneDto).subscribe((data) => {
        this.phoneVerifiedSuccess = true;
      }, (error) => {
        console.log("this is an error");
        this._router.navigate(['/auth/login'])
      });
    })
  }

}
