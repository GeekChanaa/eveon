import { AccessService } from 'src/_services/access.service';
import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { UserForLoginDto } from 'src/_models/_dtos/user-for-login-dto';
import { UserRole } from 'src/_models/_enums/user-role';
import { emailOrPhoneValidator, looksLikePhoneNumber, normalizePhoneNumber } from 'src/app/validators/email-or-phone-validator';
import { AuthService } from 'src/_services/auth.service';
import { UserService } from 'src/_services/user.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.sass']
})
export class LoginComponent implements OnInit {

  errorMessage : string = "";
  isLoading : boolean = false;
  form: FormGroup;
  // Second step, shown when the account has two factor authentication
  twoFactorToken : string | null = null;
  twoFactorForm = new FormGroup({
    code: new FormControl('', [Validators.required, Validators.maxLength(32)])
  });
  constructor(
    private access: AccessService,
    private _authService: AuthService,
    private _router : Router,
    private _snackBar : MatSnackBar,
    private _userService : UserService
  ) {
    this.form = new FormGroup({
      identifier: new FormControl('', [
        Validators.required,
        emailOrPhoneValidator()
    ]),
    password: new FormControl('', [
        Validators.required
    ])

    })
  }

  getControl(name: string): FormControl {
    return this.form.get(name) as FormControl;
  }


  ngOnInit() {
    // Coming back from a Google sign in on an account with 2FA
    const twoFactorToken = history.state?.twoFactorToken;
    if (typeof twoFactorToken === 'string' && twoFactorToken) this.twoFactorToken = twoFactorToken;
  }

  login(){

    if(this.form.invalid){
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = "";
    const formValue = this.form.value;
    const identifier : string = (formValue.identifier || '').trim();

    // The API reads either property, so send the one the user actually typed.
    var userForLogin : UserForLoginDto = looksLikePhoneNumber(identifier)
      ? { phone : normalizePhoneNumber(identifier), password : formValue.password }
      : { email : identifier.toLowerCase(), password : formValue.password };

    this._authService.login(userForLogin).subscribe((data : any) => {
      this.isLoading = false;
      if(data?.requiresTwoFactor){
        this.twoFactorToken = data.twoFactorToken;
        return;
      }
      this.navigateAfterLogin(data);
    },(error) => {
      this.isLoading = false;
      if(error.status == 429){
        this.errorMessage = "Too many failed login attempts. Please wait a while before trying again.";
      }
      else if(error.error?.error == "A partner account should login from the partner portal"){
        this.errorMessage = "A partner account should login from the partner portal"
      }
      else if(error.status == 401){
        this.errorMessage = "Email, phone number or password incorrect";
      }
      else{
        this.errorMessage = "Server error, please try again later";
      }
    })
  }

  verifyTwoFactor(){
    if(this.twoFactorForm.invalid || !this.twoFactorToken){
      this.twoFactorForm.markAllAsTouched();
      return;
    }
    this.isLoading = true;
    this.errorMessage = "";
    this._authService.verifyTwoFactor(this.twoFactorToken, (this.twoFactorForm.value.code || '').trim()).subscribe({
      next: (data : any) => { this.isLoading = false; this.navigateAfterLogin(data); },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = error.status == 429 ? "Too many attempts. Please wait a minute before trying again."
          : error.error?.error ?? "Invalid or expired authentication code";
        if(error.error?.error?.includes('sign in again')) this.cancelTwoFactor();
      }
    });
  }

  cancelTwoFactor(){
    this.twoFactorToken = null;
    this.twoFactorForm.reset();
  }

  getTwoFactorControl(): FormControl {
    return this.twoFactorForm.get('code') as FormControl;
  }

  private navigateAfterLogin(result : any){
    this.access.load().subscribe({
      next: info => {
        if(result?.twoFactorEnrollmentRequired){
          this._router.navigateByUrl('/dashboard/profile?tab=security');
          return;
        }
        this._router.navigateByUrl(info.isAdmin || info.permissions.includes('AccessDashboard') ? (this.access.canUrl('/dashboard') ? '/dashboard' : '/dashboard/profile') : '/my-dashboard');
      },
      error: () => this._router.navigateByUrl('/access-denied')
    });
  }

  googleLogin(){
    this._authService.googleLogin();
  }

}
