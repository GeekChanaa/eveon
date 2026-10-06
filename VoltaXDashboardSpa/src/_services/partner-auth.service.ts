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

  /** Always succeeds: the API never tells whether the address belongs to a partner. */
  resetPasswordRequest(email : string){
    return this.http.post(this.baseUrl+"request-reset", { email });
  }

  /** Sets the new password with the single use token from the reset link. */
  resetPassword(dto : UserForResetPasswordDto){
    return this.http.post(this.baseUrl+"reset", dto);
  }

  /** Resolves with the API response; requiresTwoFactor means a code is still needed. */
  login(model:any){
    return this.http.post(this.baseUrl +'login', model).pipe(
      map((response:any) => {
        if(response?.token){
          this.storeLoginResult(response);
        }
        return response;
      })
    )
  }

  /** Second sign in step, shared with the customer portal. */
  verifyTwoFactor(twoFactorToken: string, code: string){
    return this.http.post(environment.apiUrl + '/api/auth/verify-2fa', { twoFactorToken, code }).pipe(
      map((response:any) => {
        if(response?.token){
          this.storeLoginResult(response);
        }
        return response;
      })
    );
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
  completeExternalLogin(token: string) {
    this.storeLoginResult({ token: token });
    return this.decodedToken;
  }

  private storeLoginResult(result: any) {
    this._tokenStorage.startSession(result);
    this.token = result.token;
    this.decodedToken = this.jwtHelper.decodeToken(result.token);
  }

}
