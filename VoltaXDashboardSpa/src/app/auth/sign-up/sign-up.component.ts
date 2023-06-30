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
  styleUrls: ['./sign-up.component.css']
})
export class SignUpComponent implements OnInit {

  form : FormGroup;

  signUpFormValid : Boolean = false;

  // constructor
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

  // On init cycle hook
  ngOnInit() {
  }

  // sign up function
  register(){
    if(!this.form.valid){
      console.log("this is not valid");
      this.signUpFormValid = false;
    }
    else{
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
        // snack bar message
        this._snackBar.open("User Registered Success","dismiss",{duration:2000});
        // routing to the login page
        this._route.navigate(['/auth/login']);
      })
    }
    
  }


}
