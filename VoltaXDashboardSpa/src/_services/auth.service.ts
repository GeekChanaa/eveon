import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, map } from 'rxjs';
import { User } from 'src/_models/user';
import { UserService } from './user.service';
import { environment } from 'src/environments/environment';
import { JwtHelperService } from '@auth0/angular-jwt';
import { UserForRegisterDto } from 'src/_models/_dtos/user-for-register-dto';

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
  register(model:UserForRegisterDto) : Observable<User>{
    return this.http.post<User>(this.baseUrl + 'register', model);
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
    if(token != null)
    this.decodedToken = this.jwtHelper.decodeToken(token);
    return this.decodedToken;
  }

  // Changing password
  changePassword(pwd : any){
    return this.http.post(this.baseUrl+"ChangePassword",pwd);
  }
}
