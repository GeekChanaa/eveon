import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { UserForResetPasswordDto } from 'src/_models/_dtos/user-for-reset-password-dto';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { AuthService } from 'src/_services/auth.service';
import { passwordMatchValidator } from 'src/app/validators/password-match-validator';
import { strongPasswordValidator } from 'src/app/validators/strong-password-validator';

@Component({
  selector: 'app-reset-password',
  templateUrl: './reset-password.component.html',
  styleUrls: ['./reset-password.component.sass']
})
export class ResetPasswordComponent implements OnInit {

  form : FormGroup;
  isLoading : boolean = false;

  // Fields
  userForReset : UserForResetPasswordDto = {
    email : "",
    token : "",
    password : "",
  };
  

  constructor(
    private _authService : AuthService,
    private _route : ActivatedRoute,
    private _modalService : ActionModalService,
    private _router : Router
  ) { 
    this.form = new FormGroup({
          password: new FormControl('', [
            Validators.required,
            Validators.minLength(8),
            strongPasswordValidator()
          ]),
          confirmPassword: new FormControl('', [Validators.required])
        }, {
          validators: passwordMatchValidator('password', 'confirmPassword')
        });
  }

  ngOnInit() {
    this._route.queryParams.subscribe((params) => {
      this.userForReset.token = params['token'];
      this.userForReset.email = params['email'];
    })
  }

  //resetting the password
  resetPassword(){
    this.isLoading = true;
    this.userForReset.password = this.form.value.password;
    this._authService.resetPassword(this.userForReset).subscribe((data) => {
      this.isLoading = false;
      this._router.navigateByUrl('/auth/login');
      this._modalService.popup(ActionModalStatusEnum.Success,"Reset Email Sent","Check your email for the code we've sent you",4000);
    },(error) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Error,"Error ! ", "Something went wrong, please try again later", 4000);
    });
  }

  getControl(name: string): FormControl {
    return this.form.get(name) as FormControl;
  }

}
