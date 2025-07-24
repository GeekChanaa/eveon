import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { UserForLoginDto } from 'src/_models/_dtos/user-for-login-dto';
import { UserRole } from 'src/_models/_enums/user-role';
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
  constructor(
    private _authService: AuthService,
    private _router : Router,
    private _snackBar : MatSnackBar,
    private _userService : UserService
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
    this._authService.login(userForLogin).subscribe((data) => {
      this.isLoading = false;
      let decodedToken = this._authService.getAuthInformation();
      var userID = parseInt(decodedToken.nameid);
      var role = decodedToken.role;
      if (role && role.toLowerCase().includes("admin")) {
        this._router.navigateByUrl("/dashboard");
      }
      else{
        this._router.navigateByUrl("/my-dashboard");
      }
    },(error) => {
      this.isLoading = false;
      console.log("this is the error my friend");
      console.log(error);
      if(error.status == 401){
        this.errorMessage = "Email or password incorrect";
      }
      else if(error.error.error == 'Too many failed attempts'){
        this.errorMessage = "Too Many Failed attempts, please check your email.";
      }
      else{
        this.errorMessage = "Server error, please try again later";
      }
    })
  }

  googleLogin(){
    this._authService.googleLogin();
  }

}
