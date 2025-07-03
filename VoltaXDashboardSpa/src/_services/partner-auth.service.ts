import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { JwtHelperService } from '@auth0/angular-jwt';
import { map, Observable } from 'rxjs';
import { UserForRegisterDto } from 'src/_models/_dtos/user-for-register-dto';
import { UserForResetPasswordDto } from 'src/_models/_dtos/user-for-reset-password-dto';
import { UserPasswordChangeDto } from 'src/_models/_dtos/user-password-change-dto';
import { VerifyEmailDto } from 'src/_models/_dtos/verify-email-dto';
import { VerifyPhoneDto } from 'src/_models/_dtos/verify-phone-dto';
import { User } from 'src/_models/user';
import { environment } from 'src/environments/environment';
import { UserService } from './user.service';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class PartnerAuthService {

  baseUrl = environment.apiUrl+"/api/PartnerAuth/";
  jwtHelper = new JwtHelperService();
  decodedToken : any;
  token : any;

  constructor(
    private http: HttpClient, 
    private _userService: UserService, 
    private router: Router,
    ) 
    { }

  resetPasswordRequest(email : string){
    return this.http.get(this.baseUrl+"ResetPartnerPasswordRequest?email="+email);
  }



}
