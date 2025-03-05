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

  checkToken(token :string) : Observable<any>{
    return this.http.post(this.baseUrl+"checktoken", token);
  }

  loggedIn(){
    const token = localStorage.getItem('token');
    if(token) return !this.jwtHelper.isTokenExpired(token);
    else return false;
  }

  logout() {
    this.router.navigate(['/goodbye']).then(() => {
      setTimeout(() => {
        localStorage.removeItem("token");
      }, 4000);
    });
  }

  getAuthInformation(){
    var token = localStorage.getItem('token');
    if(token != null){
      this.decodedToken = this.jwtHelper.decodeToken(token);
    }
    return this.decodedToken;
  }

  getRole(){
    this.getAuthInformation();
    return this.decodedToken.role;
  }

  changePassword(pwd : UserPasswordChangeDto){
    return this.http.post(this.baseUrl+"ChangePassword",pwd);
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

  verifyPhone(verifyPhoneDto : VerifyPhoneDto){
    return this.http.post(this.baseUrl+"VerifyPhone",verifyPhoneDto);
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
      if(user == null)
        resolve(null);
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

  // GoogleLogin
  googleLogin() {
    window.location.href = this.baseUrl + "google-login";
  }
  
}
