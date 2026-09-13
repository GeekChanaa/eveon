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
import { GoogleLoginDto } from 'src/_models/_dtos/google-login-dto';
import { TokenStorageService } from './token-storage.service';
import { TokenRefreshService } from './token-refresh.service';

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
    private _tokenStorage: TokenStorageService,
    private _tokenRefresh: TokenRefreshService,
    ) 
    { }

  resetPasswordRequest(email : string){
    return this.http.get(this.baseUrl+"ResetPartnerPasswordRequest?email="+email);
  }

  login(model:any){
    return this.http.post(this.baseUrl +'login', model).pipe(
      map((response:any) => {
        const user = response;
        if(user){
          this.storeLoginResult(user);
        }
      })
    )
  }

  /** Partner sessions refresh through the shared /api/auth/refresh endpoint. */
  refreshSession(){
    return this._tokenRefresh.refresh();
  }

  logout(){
    this._tokenRefresh.revoke().subscribe();
    this.token = null;
    this.decodedToken = null;
    this.router.navigateByUrl('/partner-auth/login');
  }

  /**
   * Redirect flow for the partner portal. Comes back on
   * /partner-auth/google-callback with a VoltaX token.
   */
  googleLogin(returnUrl?: string) {
    let url = this.baseUrl + "google-login";
    if (returnUrl) {
      url += "?returnUrl=" + encodeURIComponent(returnUrl);
    }
    window.location.href = url;
  }

  /** Token flow (Google Identity Services button). */
  googleLoginWithIdToken(idToken: string) {
    const dto: GoogleLoginDto = { idToken: idToken };
    return this.http.post(this.baseUrl + "google", dto).pipe(
      map((response: any) => {
        if (response && response.token) {
          this.storeLoginResult(response);
        }
        return response;
      })
    );
  }

  /** Stores the tokens handed back by /partner-auth/google-callback. */
  completeExternalLogin(token: string, refreshToken?: string) {
    this.storeLoginResult({ token: token, refreshToken: refreshToken });
    return this.decodedToken;
  }

  private storeLoginResult(result: any) {
    this._tokenStorage.clear();
    this._tokenStorage.store(result);
    this.token = result.token;
    this.decodedToken = this.jwtHelper.decodeToken(result.token);
  }

}
