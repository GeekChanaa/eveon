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
    ) 
    { }

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

  /**
   * Keeps both tokens of a session. The refresh token is what survives the hour long
   * access token, so dropping it here would silently end the session.
   */
  private storeLoginResult(result: any){
    this._tokenStorage.clear();
    this._tokenStorage.store(result);
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
    // Revoke first, while the refresh token is still stored, then clear locally
    // whatever the API answered.
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
    return this.http.get(this.baseUrl+"resetpassword?email="+email);
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

  getUserLocation(): Promise<{latitude: number, longitude: number, city : string, country : string}> {
    return new Promise((resolve, reject) => {
      const lastRetrievedStr = localStorage.getItem("lastRetrieved");
      let lastRetrieved = null;  
      if(lastRetrievedStr != null && lastRetrievedStr != "")
        lastRetrieved = Date.parse(lastRetrievedStr);

      // Get the current timestamp
      const now = new Date().getTime();
      const sixHoursInMilliseconds = 6 * 60 * 60 * 1000;
      if(localStorage.getItem("userLatitude") != '' && lastRetrieved && now - lastRetrieved < sixHoursInMilliseconds){
        resolve({ 
          latitude: parseFloat(localStorage.getItem("userLongitude") ?? "0"),
          longitude: parseFloat(localStorage.getItem("userLatitude") ?? "0"),
          city : localStorage.getItem("userLocationCity") ?? "World",
          country : localStorage.getItem("userLocationCountry") ?? "World"
        });
      }
      else if (navigator.geolocation) {
        navigator.geolocation.getCurrentPosition(position => {
          this.getLocationDetails(position.coords.latitude, position.coords.longitude).then((locationDetails : any) => {
            localStorage.setItem("userLongitude",position.coords.longitude.toString());
            localStorage.setItem("userLatitude",position.coords.latitude.toString());
            localStorage.setItem("userLocationCity",locationDetails.city);
            localStorage.setItem("userLocationCountry",locationDetails.country);
            localStorage.setItem("lastRetrieved", now.toString());
            resolve({ 
                latitude: position.coords.latitude, 
                longitude: position.coords.longitude,
                city : locationDetails.city,
                country : locationDetails.country
              });
          })
        }, err => {
          reject(err);
        });
      } else {
        reject('Geolocation is not supported by this browser.');
      }
    });
  }


  getLocationDetails(latitude : number, longitude : number) {
    // Construct the API request URL
    const url = `https://maps.googleapis.com/maps/api/geocode/json?latlng=${latitude},${longitude}&key=AIzaSyASK_Y37ctDVZfa9P7OqJ2QsFpC_XMgZBQ`;
  
    return fetch(url)
      .then(response => response.json())
      .then(data => {
        if (data.status === 'OK') {
          // Extract the country and city from the API response
          const results = data.results[0].address_components;
          const country = results.find((component : any) => component.types.includes('country'));
          const city = results.find((component : any) => component.types.includes('locality'));
  
          return {
            country: country ? country.long_name : '',
            city: city ? city.long_name : ''
          };
        } else {
          throw new Error('Unable to retrieve location details');
        }
      })
      .catch(error => {
        console.error('Error in fetching location details:', error);
      });
  }

  getUserInformations() : Promise<any>{
    return new Promise((resolve, reject) => {
      const user = this.getAuthInformation(); // Assuming this method exists in authService
      if(user == null) {
        resolve(null);
        return;
      }
      // Map the token information to your UserInformation model
      let userInformations: any = {
        isEmailVerified: user.IsEmailVerified,
        isPhoneNumberVerified: user.IsPhoneNumberVerified,
        lastName: user.family_name,
        firstName: user.given_name,
        userID: parseInt(user.nameid),
        role: user.role,
        email: user.unique_name,
      };
      if(localStorage.getItem("userLongitude") != null && localStorage.getItem("userLongitude") != ''){
        userInformations.latitude =  localStorage.getItem("userLatitude");
        userInformations.longitude =  localStorage.getItem("userLongitude");
        userInformations.country =  localStorage.getItem("userLocationCountry");
        userInformations.city =  localStorage.getItem("userLocationCity");
        resolve(userInformations);
      }
      else{
        this.getUserLocation().then(location => {
          // setting localStorage Location
          userInformations.country = location.country;
          userInformations.city = location.city;
          localStorage.setItem("userLocationCountry", location.country);
          localStorage.setItem("userLocationCity", location.city);
          localStorage.setItem("userLatitude",location.latitude.toString());
          localStorage.setItem("userLongitude",location.longitude.toString());
          resolve(userInformations);
          // Resolve with complete user information
        }).catch(error => {
          reject(error);
        });
      }
      
    });
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
   * Redirect flow used to attach a Google account to the signed in profile.
   * The current JWT travels as a ticket because a top level navigation
   * cannot carry an Authorization header.
   */
  startGoogleLink(returnUrl?: string) {
    // The ticket is validated as a live JWT, so an expired one has to be renewed
    // before the browser leaves the app.
    if (this._tokenStorage.isAccessTokenExpired() && this._tokenStorage.hasRefreshToken()) {
      this._tokenRefresh.refresh().subscribe({
        next: () => this.redirectToGoogleLink(returnUrl),
        error: () => this.router.navigateByUrl('/auth/login')
      });
      return;
    }

    this.redirectToGoogleLink(returnUrl);
  }

  private redirectToGoogleLink(returnUrl?: string) {
    const token = this._tokenStorage.accessToken;
    if (token == null) {
      this.router.navigateByUrl('/auth/login');
      return;
    }

    let url = this.baseUrl + "google-link?ticket=" + encodeURIComponent(token);
    if (returnUrl) {
      url += "&returnUrl=" + encodeURIComponent(returnUrl);
    }
    window.location.href = url;
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

  /** Stores the tokens handed back by /auth/google-callback. */
  completeExternalLogin(token: string, refreshToken?: string) {
    this.storeLoginResult({ token: token, refreshToken: refreshToken });
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
