import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { AuthService } from 'src/_services/auth.service';
import { UserService } from 'src/_services/user.service';

enum VerifyEmailFormStepEnum{
  Verification = 0,
  Verified = 1
}
@Component({
  selector: 'app-profile-verify-email',
  templateUrl: './profile-verify-email.component.html',
  styleUrls: ['./profile-verify-email.component.sass']
})
export class ProfileVerifyEmailComponent implements OnInit {


  verifyEmailForm : FormGroup;

  VerifyEmailFormStepEnum = VerifyEmailFormStepEnum;

  @Input() userID : number = 0;
  @Input() email : string = "";
  @Input() isLoading : boolean = true;

  @Output() closeEvent : EventEmitter<void> = new EventEmitter();

  formStep : VerifyEmailFormStepEnum = VerifyEmailFormStepEnum.Verification;

  constructor(
    private _userService : UserService,
    private _authService : AuthService,
    private _modalService : ActionModalService
  ) { 
    this.verifyEmailForm = new FormGroup({
      code : new FormControl("")
    })
  }

  ngOnInit() {
  }

  getControl(form : FormGroup,name: string): FormControl {
    return form.get(name) as FormControl;
  }

  verifyEmail(){
    this._authService.verifyEmail({email : this.email, token: this.verifyEmailForm.value.code}).subscribe((data) => {
      this._modalService.popup(ActionModalStatusEnum.Success, "Success ! ", "Email Verified Successfully",4000);
      this.formStep = VerifyEmailFormStepEnum.Verified;
    },(error) =>{
      this._modalService.popup(ActionModalStatusEnum.Error,"Error !","Something Went wrong please try again later", 4000);
    })
  }

  closeModal(){
    this.closeEvent.emit();
  }
}
