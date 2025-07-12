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
  }

  login(){
    this.isLoading = true;
    const formValue = this.form.value;
    var userForLogin : UserForLoginDto = {
      email : formValue.email,
      password : formValue.password
    };
    this._partnerAuthService.login(userForLogin).subscribe((data) => {
      this.isLoading = false;
      this._router.navigateByUrl('/partner-dashboard')  
    },(error) => {
      this.isLoading = false;
      console.log("this is the error : ");
      console.log(error);
      if(error.status == 401){
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
}
