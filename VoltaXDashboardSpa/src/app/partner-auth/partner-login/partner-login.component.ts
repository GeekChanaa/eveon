import { Component, OnInit } from '@angular/core';
import { FormGroup, FormControl, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { UserForLoginDto } from 'src/_models/_dtos/user-for-login-dto';
import { AuthService } from 'src/_services/auth.service';
import { PartnerAuthService } from 'src/_services/partner-auth.service';
import { UserService } from 'src/_services/user.service';

@Component({
  selector: 'app-partner-login',
  templateUrl: './partner-login.component.html',
  styleUrls: ['./partner-login.component.sass']
})
export class PartnerLoginComponent implements OnInit {
  errorMessage : string = "";
  isLoading : boolean = false;
  form: FormGroup;
  twoFactorToken : string | null = null;
  twoFactorForm = new FormGroup({
    code: new FormControl('', [Validators.required, Validators.maxLength(32)])
  });
  constructor(
    private _router : Router,
    private _partnerAuthService : PartnerAuthService
  ) {
    this.form = new FormGroup({
      email: new FormControl('', [
        Validators.required,
        Validators.email
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
    this.isLoading = true;
    const formValue = this.form.value;
    var userForLogin : UserForLoginDto = {
      email : formValue.email,
      password : formValue.password
    };
    this._partnerAuthService.login(userForLogin).subscribe((data : any) => {
      this.isLoading = false;
      if(data?.requiresTwoFactor){
        this.twoFactorToken = data.twoFactorToken;
        return;
      }
      this.navigateAfterLogin(data);
    },(error) => {
      this.isLoading = false;
      if(error.status == 429){
        this.errorMessage = "Too many attempts. Please wait a while before trying again.";
      }
      else if(error.status == 401){
        this.errorMessage = "Email or password incorrect";
      }
      else if(error.error.error == 'Too many failed attempts'){
        this.errorMessage = "Too Many Failed attempts, please check your email.";
      }
      else{
        this.errorMessage = error.error.error;
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
    this._partnerAuthService.verifyTwoFactor(this.twoFactorToken, (this.twoFactorForm.value.code || '').trim()).subscribe({
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
    this._router.navigateByUrl(result?.twoFactorEnrollmentRequired ? '/partner-dashboard/security' : '/partner-dashboard');
  }

  googleLogin(){
    this._partnerAuthService.googleLogin();
  }
}
