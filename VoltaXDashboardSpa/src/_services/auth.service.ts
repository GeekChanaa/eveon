import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, map } from 'rxjs';
import { User } from 'src/_models/user';
import { UserService } from './user.service';
import { environment } from 'src/environments/environment';
import { JwtHelperService } from '@auth0/angular-jwt';
import { UserForRegisterDto } from 'src/_models/_dtos/user-for-register-dto';
import { UserForResetPasswordDto } from 'src/_models/_dtos/user-for-reset-password-dto';
import { UserPasswordChangeDto } from 'src/_models/_dtos/user-password-change-dto';
import { VerifyEmailDto } from 'src/_models/_dtos/verify-email-dto';
import { VerifyPhoneDto } from 'src/_models/_dtos/verify-phone-dto';
import { AddPhoneNumberDto } from 'src/_models/_dtos/add-phone-number-dto';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  baseUrl = environment.apiUrl+"/api/auth/";
  jwtHelper = new JwtHelperService();
  decodedToken : any;
  token : any;

  constructor(
    private http: HttpClient, 
    private _userService: UserService, 
    private router: Router,
    ) 
    { }

  // Login method
  login(model:any){
    return this.http.post(this.baseUrl +'login', model).pipe(
      map((response:any) => {
        const user = response;
        if(user){
          localStorage.setItem('token',user.token);
          this.token = user.token;
          var decode = this.jwtHelper.decodeToken(user.token);
          this.decodedToken = decode;
        }
      })
    )
  }

  // Register method
  register(model:UserForRegisterDto) {
    return this.http.post<User>(this.baseUrl + 'register', model).pipe(
      map((response:any) => {
        console.log("this is the response");
        console.log(response);
        const user = response;
        if(user){
          localStorage.setItem('token',user.token);
          this.token = user.token;
          const decode = this.jwtHelper.decodeToken(user.token);
          this.decodedToken = decode;
        }
      })
    );
  }

  // Checking if token is valid or not
  checkToken(token :string) : Observable<any>{
    return this.http.post(this.baseUrl+"checktoken", token);
  }

  // Check if user is logged in
  loggedIn(){
    const token = localStorage.getItem('token');
    if(token) return !this.jwtHelper.isTokenExpired(token);
    else return false;
  }

  logout(){
    localStorage.removeItem("token");
    this.router.navigate(['/']);
  }

  getAuthInformation(){
    var token = localStorage.getItem('token');
    if(token != null){
      this.decodedToken = this.jwtHelper.decodeToken(token);
    }
    return this.decodedToken;
  }

  // getting user role
  getRole(){
    this.getAuthInformation();
    return this.decodedToken.role;
  }

  // Changing password
  changePassword(pwd : UserPasswordChangeDto){
    return this.http.post(this.baseUrl+"ChangePassword",pwd);
  }

  // Reset password request
  resetPasswordRequest(email : string){
    return this.http.get(this.baseUrl+"resetpassword?email="+email);
  }

  // Reset password
  resetPassword(user : UserForResetPasswordDto){
    return this.http.post(this.baseUrl+"ResetPassword", user);
  }

  // verify Email
  verifyEmail(verifyEmailDto : VerifyEmailDto){
    return this.http.post(this.baseUrl+"VerifyEmail",verifyEmailDto);
  }

  // verify phone
  verifyPhone(verifyPhoneDto : VerifyPhoneDto){
    return this.http.post(this.baseUrl+"VerifyPhone",verifyPhoneDto);
  }

  // send phone verification sms
  sendPhoneVerificationSms(AddPhoneNumberDto : AddPhoneNumberDto){
    return this.http.post(this.baseUrl+"SendPhoneVerificationSMS",AddPhoneNumberDto);
  }

  // send email verification sms
  sendEmailVerificationCode(userID : number){
    return this.http.post(this.baseUrl+"SendEmailVerificationCode",userID);
  }
}
