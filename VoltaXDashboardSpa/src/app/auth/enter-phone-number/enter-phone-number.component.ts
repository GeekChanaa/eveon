import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from 'src/_services/auth.service';

@Component({
  selector: 'app-enter-phone-number',
  templateUrl: './enter-phone-number.component.html',
  styleUrls: ['./enter-phone-number.component.css']
})
export class EnterPhoneNumberComponent implements OnInit {

  form : FormGroup;
  email : string = "";

  // Constructor
  constructor(
    private _snackBar : MatSnackBar,
    private _authService : AuthService,
    private _route : ActivatedRoute,
    private _router : Router
  ) { 
    this.form = new FormGroup({
      phone: new FormControl('', [
        Validators.required,
        Validators.pattern('^((\\+91-?)|0)?[0-9]{10}$') // adjust pattern as needed
      ])
    })
  }

  // on init cycle hook
  ngOnInit() {
    this._route.queryParams.subscribe((params) => {
      this.email = params['email'];
    })
  }

  sendPhoneVerification(){
    var addPhoneNumberDto : any = {};
    addPhoneNumberDto.email = this.email;
    addPhoneNumberDto.phone = this.form.value.phone;
    this._authService.sendPhoneVerificationSms(addPhoneNumberDto).subscribe((data) => {
        this._snackBar.open("Phone verification SMS Sent","dismiss",{duration:2000});
        console.log("phone verification sms sent");
        this._router.navigate(['/auth/verify-phone?phone='+this.form.value.phone]);
    })
  }

}
