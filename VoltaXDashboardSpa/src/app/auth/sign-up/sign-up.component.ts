import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import {  Router } from '@angular/router';
import { UserForRegisterDto } from 'src/_models/_dtos/user-for-register-dto';
import { AuthService } from 'src/_services/auth.service';

export function passwordMatchValidator(passwordField: string, confirmPasswordField: string): ValidatorFn {
  return (control: AbstractControl): {[key: string]: any} | null => {
    const password = control.get(passwordField);
    const confirmPassword = control.get(confirmPasswordField);
    if (!password || !confirmPassword) {
      return null;
    }
    return password.value === confirmPassword.value ? null : { 'passwordMismatch': true };
  };
}

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


  constructor( 
    private _authService : AuthService,
    private _snackBar : MatSnackBar,
    private _route : Router
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
        Validators.minLength(8) 
      ]),
      confirmPassword: new FormControl('', [
        Validators.required
      ])
    }, { validators: passwordMatchValidator('password','confirmPassword') });
    
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
      password: formValue.password
    }

    this._authService.register(userForRegister).subscribe((data) => {
      this.isLoading = false;
      this._route.navigate(['/auth/verification-mail-sent']);
    },(error) => {
      this.isLoading = false;
      this.errorMessage = error.error.error;
    })
    
  }


}
