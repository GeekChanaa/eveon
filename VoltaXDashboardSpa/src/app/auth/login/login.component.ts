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

  // On init cycle hook
  ngOnInit() {
  }

  // Login button
  login(){

    this.isLoading = true;
    const formValue = this.form.value;
    var userForLogin : UserForLoginDto = {
      email : formValue.email,
      password : formValue.password
    };
    this._authService.login(userForLogin).subscribe((data) => {
      this.isLoading = false;
      var userID = parseInt(this._authService.getAuthInformation().nameid);
      this._userService.getById(userID).subscribe((u) => {
        console.log("this is the user");
        console.log(u);
        if(u.role == "Admin")
          this._router.navigate(['/dashboard']);
        else if(u.role == "Customer")
          this._router.navigate(['/my-dashboard']);
        else if(u.role == "Partner")
          this._router.navigate(['/partner-dashboard']);
        this._snackBar.open("Welcome Back","dismiss",{duration:2000});
      });
      
    },(error) => {
      this.isLoading = false;
      if(error.status == 401){
        this.errorMessage = "Email or password incorrect";
      }
      else{
        this.errorMessage = "Server error, please try again later";
      }
    })
  }

}
