import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, map, tap, throwError } from 'rxjs';
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
import { GoogleLoginDto } from 'src/_models/_dtos/google-login-dto';
import { LinkedAccountsDto } from 'src/_models/_dtos/linked-accounts-dto';
import { TokenStorageService } from './token-storage.service';
import { TokenRefreshService } from './token-refresh.service';
import { GeolocationService } from './geolocation.service';

export interface TwoFactorStatus { eligible: boolean; enabled: boolean; required: boolean; recoveryCodesLeft: number; }
export interface TwoFactorEnrollment { secret: string; otpAuthUri: string; qrCodeDataUri: string; }

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
    private _tokenStorage: TokenStorageService,
    private _tokenRefresh: TokenRefreshService,
    private _geolocation: GeolocationService,
    ) 
    { }

  /**
   * Resolves with the API response. When it carries requiresTwoFactor no session exists
   * yet: the caller asks for the code and calls {@link verifyTwoFactor}.
   */
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

  /** Second sign in step: the challenge token from the login response plus a TOTP or recovery code. */
  verifyTwoFactor(twoFactorToken: string, code: string){
    return this.http.post(this.baseUrl + 'verify-2fa', { twoFactorToken, code }).pipe(
      map((response:any) => {
        if(response?.token){
          this.storeLoginResult(response);
        }
        return response;
      })
    );
  }

  register(model:UserForRegisterDto) {
    return this.http.post<User>(this.baseUrl + 'register', model).pipe(
      map((response:any) => {
        const user = response;
        if(user){
          this.storeLoginResult(user);
        }
      })
    );
  }

  /** The access token stays in memory; the refresh token was set by the API as an HttpOnly cookie. */
  private storeLoginResult(result: any){
    this._tokenStorage.startSession(result);
    this.token = result.token;
    this.decodedToken = this.jwtHelper.decodeToken(result.token);
  }

  /** Swaps the refresh token for a new pair. Shares one in-flight call across callers. */
  refreshSession(){
    return this._tokenRefresh.refresh();
  }

  checkToken(token :string) : Observable<any>{
    return this.http.post(this.baseUrl+"checktoken", token);
  }

  /**
   * An expired access token no longer means "signed out": as long as a refresh token is
   * held, the interceptor will get a new access token on the next call.
   */
  loggedIn(){
    if(!this._tokenStorage.isAccessTokenExpired()) return true;
    return this._tokenStorage.hasRefreshToken();
  }

  logout() {
    // Revoke the refresh cookie server side, then clear locally whatever the API answered.
    this._tokenRefresh.revoke().subscribe();
    this.clearSession();

    this.router.navigate(['/goodbye']);
  }

  /** Ends every session of this account, not only the one in this browser. */
  logoutEverywhere(){
    return this.http.post(this.baseUrl + "LogoutEverywhere", {}).pipe(tap(() => {
      this.clearSession();
      this.router.navigateByUrl('/auth/login');
    }));
  }

  private clearSession(){
    this._tokenStorage.clear();
    this.token = null;
    this.decodedToken = null;
  }

  getAuthInformation(){
    var token = this._tokenStorage.accessToken;
    this.decodedToken = null;
    if(token != null){
      try { this.decodedToken = this.jwtHelper.decodeToken(token); } catch { this.decodedToken = null; }
    }
    return this.decodedToken;
  }

  getRole(){
    this.getAuthInformation();
    return this.decodedToken?.role;
  }

  changePassword(pwd : UserPasswordChangeDto){
    return this.http.post(this.baseUrl+"ChangePassword",pwd).pipe(tap(() => this.logout()));
  }

  resetPasswordRequest(email : string){
    return this.http.post(this.baseUrl+"request-password-reset", { email });
  }

  resetPassword(user : UserForResetPasswordDto){
    return this.http.post(this.baseUrl+"ResetPassword", user);
  }

  verifyEmail(verifyEmailDto : VerifyEmailDto){
    return this.http.post(this.baseUrl+"VerifyEmail",verifyEmailDto);
  }

  verifyPhone(verifyPhoneDto: VerifyPhoneDto): Observable<any> {
    return this.http.post<{ token: string }>(this.baseUrl + "VerifyPhone", verifyPhoneDto).pipe(
        tap(response => {
          if (response && response.token) {
            // Only the access token changes here; the refresh token stays valid.
            this._tokenStorage.storeAccessToken(response.token);
            this.token = response.token;
            this.decodedToken = this.jwtHelper.decodeToken(response.token);
          }
        }),
        catchError((error) => {
          console.error("Phone verification failed", error);
          return throwError(() => error);
        })
      );
  }

  sendPhoneVerificationSms(AddPhoneNumberDto : AddPhoneNumberDto){
    return this.http.post(this.baseUrl+"SendPhoneVerificationSMS",AddPhoneNumberDto);
  }

  sendEmailVerificationCode(userID : number){
    return this.http.post(this.baseUrl+"SendEmailVerificationCode",userID);
  }

  /** Sends a new verification link to the signed in user's address. */
  resendVerificationEmail(){
    return this.http.post(this.baseUrl+"resend-verification", {});
  }

  // ---------------------------------------------------------------------
  // Two factor authentication (admin and partner accounts)
  // ---------------------------------------------------------------------

  getTwoFactorStatus(): Observable<TwoFactorStatus> {
    return this.http.get<TwoFactorStatus>(this.baseUrl + "2fa");
  }

  enrollTwoFactor(): Observable<TwoFactorEnrollment> {
    return this.http.post<TwoFactorEnrollment>(this.baseUrl + "2fa/enroll", {});
  }

  confirmTwoFactor(code: string): Observable<{ recoveryCodes: string[] }> {
    return this.http.post<{ recoveryCodes: string[] }>(this.baseUrl + "2fa/confirm", { code });
  }

  disableTwoFactor(password: string, code: string) {
    return this.http.post(this.baseUrl + "2fa/disable", { password, code });
  }

  /** Token based user information, plus the location only if the user already shared it. */
  getUserInformations() : Promise<any>{
    const user = this.getAuthInformation();
    if(user == null) return Promise.resolve(null);

    let userInformations: any = {
      isEmailVerified: user.IsEmailVerified,
      isPhoneNumberVerified: user.IsPhoneNumberVerified,
      lastName: user.family_name,
      firstName: user.given_name,
      userID: parseInt(user.nameid),
      role: user.role,
      email: user.unique_name,
    };
    const location = this._geolocation.cachedLocation();
    if(location){
      userInformations = { ...userInformations, ...location };
    }
    return Promise.resolve(userInformations);
  }

  // ---------------------------------------------------------------------
  // Google sign in
  // ---------------------------------------------------------------------

  /**
   * Redirect flow: hands the browser over to the API, which bounces it to Google
   * and finally back to /auth/google-callback with a VoltaX token.
   */
  googleLogin(returnUrl?: string) {
    let url = this.baseUrl + "google-login";
    if (returnUrl) {
      url += "?returnUrl=" + encodeURIComponent(returnUrl);
    }
    window.location.href = url;
  }

  /**
   * Redirect flow used to attach a Google account to the signed in profile. The API
   * hands out a one time, 60 second ticket; only that ticket travels in the URL, never
   * the access token.
   */
  startGoogleLink(returnUrl?: string) {
    this.http.post<{ ticket: string }>(this.baseUrl + "google/link-ticket", {}).subscribe({
      next: ({ ticket }) => {
        let url = this.baseUrl + "google-link?ticket=" + encodeURIComponent(ticket);
        if (returnUrl) {
          url += "&returnUrl=" + encodeURIComponent(returnUrl);
        }
        window.location.href = url;
      },
      error: () => this.router.navigateByUrl('/auth/login')
    });
  }

  /**
   * Token flow: the client already holds a Google ID token
   * (Google Identity Services button) and swaps it for a VoltaX token.
   */
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

  /** Stores the access token handed back by /auth/google-callback (the refresh token is in the cookie). */
  completeExternalLogin(token: string) {
    this.storeLoginResult({ token: token });
    return this.getAuthInformation();
  }

  getLinkedAccounts(): Observable<LinkedAccountsDto> {
    return this.http.get<LinkedAccountsDto>(this.baseUrl + "linked-accounts");
  }

  linkGoogleWithIdToken(idToken: string): Observable<LinkedAccountsDto> {
    const dto: GoogleLoginDto = { idToken: idToken };
    return this.http.post<LinkedAccountsDto>(this.baseUrl + "google/link", dto);
  }

  unlinkGoogle(): Observable<LinkedAccountsDto> {
    return this.http.post<LinkedAccountsDto>(this.baseUrl + "google/unlink", {});
  }

}
