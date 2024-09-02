import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { UserForResetPasswordDto } from 'src/_models/_dtos/user-for-reset-password-dto';
import { AuthService } from 'src/_services/auth.service';

@Component({
  selector: 'app-reset-password',
  templateUrl: './reset-password.component.html',
  styleUrls: ['./reset-password.component.sass']
})
export class ResetPasswordComponent implements OnInit {

  // Fields
  userForReset : UserForResetPasswordDto = {
    email : "",
    token : "",
    password : "",
  };

  constructor(
    private _authService : AuthService,
    private _route : ActivatedRoute
  ) { }

  ngOnInit() {
    this._route.queryParams.subscribe((params) => {
      this.userForReset.token = params['token'];
      this.userForReset.email = params['email'];
    })
  }

  //resetting the password
  resetPassword(){
    this._authService.resetPassword(this.userForReset).subscribe((data) => {
      console.log("reset success");
    });
  }

}
