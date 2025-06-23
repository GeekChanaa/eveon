import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { UserPasswordChangeDto } from 'src/_models/_dtos/user-password-change-dto';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { AuthService } from 'src/_services/auth.service';
import { passwordMatchValidator } from 'src/app/validators/password-match-validator';
import { strongPasswordValidator } from 'src/app/validators/strong-password-validator';

@Component({
  selector: 'app-profile-security-change-password',
  templateUrl: './profile-security-change-password.component.html',
  styleUrls: ['./profile-security-change-password.component.sass']
})
export class ProfileSecurityChangePasswordComponent implements OnInit {

  changePasswordForm : FormGroup;
  isLoading : boolean = false;
  errorMessage: string = "";

  @Input() userID : number = 0;
  @Output() cancelEvent : EventEmitter<void> = new EventEmitter();

  userPasswordChange : UserPasswordChangeDto = {
      id: this.userID,
      currentPassword: '',
      newPassword: '',
      newPasswordCheck: ''
    };
  
  constructor(
    private _authService : AuthService,
    private _modalService : ActionModalService,

  ) {
    this.changePasswordForm = new FormGroup({
        oldPassword: new FormControl('', [
          Validators.required
        ]),
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

  cancel(){
    this.cancelEvent.emit();
  }

  ngOnInit() {
  }

   changePassword(){
    this.isLoading = true;
    this.userPasswordChange.id = this.userID;
    this.userPasswordChange.currentPassword = this.changePasswordForm.value.oldPassword;
    this.userPasswordChange.newPassword =  this.changePasswordForm.value.password;
    this.userPasswordChange.newPasswordCheck  = this.changePasswordForm.value.confirmPassword;
    this._authService.changePassword(this.userPasswordChange).subscribe((data) => {
      this._modalService.popup(ActionModalStatusEnum.Success,"Success !","Password Changed Successfully ! ", 4000);
      this.isLoading = false
      this.cancel();
    },(error) => {
      if(error.error == 'The current password is incorrect.')
        this.errorMessage = "The password you entered is incorrect";
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Error,"Error","Something Went wrong please try again later ! ", 4000);
    })
  }

  getControl(name: string): FormControl {
    return this.changePasswordForm.get(name) as FormControl;
  }

}
