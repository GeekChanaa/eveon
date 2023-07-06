import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AuthService } from 'src/_services/auth.service';
import { VerifyEmailDto } from 'src/_models/_dtos/verify-email-dto';

@Component({
  selector: 'app-verify-email',
  templateUrl: './verify-email.component.html',
  styleUrls: ['./verify-email.component.css']
})
export class VerifyEmailComponent implements OnInit {


  // Email verified condition
  emailVerifiedSuccess : boolean = false;

  // errors messages
  errorMessage : string = "";

  // Email concerned 
  email : string = "";

  // Constructor
  constructor(
    private _route : ActivatedRoute,
    private _authService : AuthService
  ) { }

  ngOnInit() {
    this._route.queryParams.subscribe((params) => {
      const token = params['token'];
      this.email = params['email'];
      let verifyEmailDto : VerifyEmailDto = {
        token : token,
        email : this.email
      };
      console.log("dto : ");
      console.log(verifyEmailDto);
      this._authService.verifyEmail(verifyEmailDto).subscribe((data) => {
        this.emailVerifiedSuccess = true;
      }, (error) => {
        if(error.error == 'Email or token incorrect'){
          this.emailVerifiedSuccess = false;
          this.errorMessage = error.error;
        }
      });
    })
  }

}
