import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from 'src/_services/auth.service';
import { UserService } from 'src/_services/user.service';

@Component({
  selector: 'app-enter-phone-number',
  templateUrl: './enter-phone-number.component.html',
  styleUrls: ['./enter-phone-number.component.sass']
})
export class EnterPhoneNumberComponent implements OnInit {

  form : FormGroup;
  email : string = "";
  checkingPhone : boolean = false;
  phoneTimeout : any = {};
  phoneExists : boolean = false;

  // Constructor
  constructor(
    private _snackBar : MatSnackBar,
    private _authService : AuthService,
    private _route : ActivatedRoute,
    private _router : Router,
    private _userService : UserService
  ) { 
    this.form = new FormGroup({
      phone: new FormControl('', [
        Validators.required,
        Validators.pattern('^((\\+91-?)|0)?[0-9]{10}$') 
      ])
    })
  }

  // on init cycle hook
  ngOnInit() {
    this.email = this._authService.getAuthInformation().unique_name;
  }

  sendPhoneVerification(){
    var addPhoneNumberDto : any = {};
    addPhoneNumberDto.email = this.email;
    addPhoneNumberDto.phone = this.form.value.phone;
    this._authService.sendPhoneVerificationSms(addPhoneNumberDto).subscribe((data) => {
        this._router.navigate(['/auth/verify-phone']);
    })
  }

  isPhoneUnique(email: string): void {
    this.checkingPhone = true;
    clearTimeout(this.phoneTimeout);
    this.phoneTimeout = setTimeout(() => {
      this._userService.isPhoneUnique(email).subscribe(
        (data) => {
          this.phoneExists = data;
          this.checkingPhone = false;
        },
        (error) => {
          clearTimeout(this.phoneTimeout);
        }
      );
    }, 800);
  }

  isPhoneValid(){
    return this.form.valid && !this.phoneExists && !this.checkingPhone
  }

}
