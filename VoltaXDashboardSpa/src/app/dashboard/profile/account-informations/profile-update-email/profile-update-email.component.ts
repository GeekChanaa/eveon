import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { AuthService } from 'src/_services/auth.service';
import { UserService } from 'src/_services/user.service';

enum UpdateEmailFormStepEnum{
  UpdateEmail = 0,
  VerificationChoice = 1,
  Verification = 2,
  Verified = 3
}

@Component({
  selector: 'app-profile-update-email',
  templateUrl: './profile-update-email.component.html',
  styleUrls: ['./profile-update-email.component.sass']
})
export class ProfileUpdateEmailComponent implements OnInit {

  emailToChange : string = "";

  updateEmailForm : FormGroup;
  verifyEmailForm : FormGroup;

  UpdateEmailFormStepEnum = UpdateEmailFormStepEnum;

  @Input() userID : number = 0;

  @Output() closeEvent : EventEmitter<void> = new EventEmitter();

  formStep : UpdateEmailFormStepEnum = UpdateEmailFormStepEnum.UpdateEmail;

  constructor(
    private _userService : UserService,
    private _authService : AuthService,
    private _modalService : ActionModalService
  ) { 
    this.updateEmailForm = new FormGroup({
      email : new FormControl("")
    });
    this.verifyEmailForm = new FormGroup({
      code : new FormControl("")
    })
  }

  ngOnInit() {
  }

  getControl(form : FormGroup,name: string): FormControl {
    return form.get(name) as FormControl;
  }

  saveEmail(){
    this._userService.updateEmail({id : this.userID, email: this.updateEmailForm.value.email}).subscribe((data) => {
      this._modalService.popup(ActionModalStatusEnum.Success, "Success ! ", "Email Changed Successfully",4000);
      this.formStep = UpdateEmailFormStepEnum.VerificationChoice;
    },(error) =>{
      this._modalService.popup(ActionModalStatusEnum.Error,"Error !","Something Went wrong please try again later", 4000);
    })
  }

  verifyEmail(){
    this._authService.verifyEmail({email : this.updateEmailForm.value.email, token: this.verifyEmailForm.value.code}).subscribe((data) => {
      this._modalService.popup(ActionModalStatusEnum.Success, "Success ! ", "Email Verified Successfully",4000);
      this.formStep = UpdateEmailFormStepEnum.Verified;
    },(error) =>{
      this._modalService.popup(ActionModalStatusEnum.Error,"Error !","Something Went wrong please try again later", 4000);
    })
  }

  closeModal(){
    this.closeEvent.emit();
  }

}
