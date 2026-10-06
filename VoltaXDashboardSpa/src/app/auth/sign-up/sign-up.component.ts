import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import {  Router } from '@angular/router';
import { UserForRegisterDto } from 'src/_models/_dtos/user-for-register-dto';
import { LEGAL } from 'src/app/global/legal';
import { AuthService } from 'src/_services/auth.service';
import { passwordMatchValidator } from 'src/app/validators/password-match-validator';
import { strongPasswordValidator } from 'src/app/validators/strong-password-validator';

@Component({
  selector: 'app-sign-up',
  templateUrl: './sign-up.component.html',
  styleUrls: ['./sign-up.component.sass']
})
export class SignUpComponent implements OnInit {

  form : FormGroup;

  signUpFormValid : Boolean = false;
  isLoading : boolean = false;
  errorMessage : string = "";
  emailAlreadyExists : boolean = false;


    constructor(
    private _authService: AuthService,
    private _snackBar: MatSnackBar,
    private _route: Router
  ) {
    this.form = new FormGroup({
      firstName: new FormControl('', [
        Validators.required
      ]),
      lastName: new FormControl('', [
        Validators.required
      ]),
      email: new FormControl('', [
        Validators.required,
        Validators.email
      ]),
      password: new FormControl('', [
        Validators.required,
        Validators.minLength(8),
        strongPasswordValidator()
      ]),
      confirmPassword: new FormControl('', [Validators.required]),
      acceptTerms: new FormControl(false, [Validators.requiredTrue])
    }, {
      validators: passwordMatchValidator('password', 'confirmPassword')
    });
  }

  ngOnInit() {
  }

  register(){
    if(!this.form.valid){
      this.signUpFormValid = false;
      return;
    }

    this.isLoading = true;
    this.signUpFormValid = true;
    const formValue = this.form.value;
    var userForRegister : UserForRegisterDto = {
      firstName: formValue.firstName,
      lastName: formValue.lastName,
      email: formValue.email,
      phone: '',
      password: formValue.password,
      termsVersion: LEGAL.termsVersion
    }

    this._authService.register(userForRegister).subscribe((data) => {
      this.isLoading = false;
      this._route.navigate(['/auth/verification-mail-sent']);
    },(error) => {
      if(error.error.error == "Email already exists")
      {
        this.emailAlreadyExists = true;
        this.isLoading = false;
        return;
      }
      this.isLoading = false;
      this.errorMessage = error.error.error;
    })
    
  }

  getControl(name: string): FormControl {
    return this.form.get(name) as FormControl;
  }

  // Signing up with Google creates the account on the first redirect back
  googleLogin(){
    this._authService.googleLogin();
  }

}
