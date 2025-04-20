import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { AuthService } from 'src/_services/auth.service';
import { UserService } from 'src/_services/user.service';

enum VerifyPhoneFormStepEnum{
  Verification = 0,
  Verified = 1
}

@Component({
  selector: 'app-profile-verify-phone',
  templateUrl: './profile-verify-phone.component.html',
  styleUrls: ['./profile-verify-phone.component.sass']
})
export class ProfileVerifyPhoneComponent implements OnInit {

  verifyPhoneForm : FormGroup;

  VerifyPhoneFormStepEnum = VerifyPhoneFormStepEnum;

  @Input() userID : number = 0;
  @Input() phone : string = "";
  @Input() email : string = "";
  @Input() isLoading : boolean = true;

  @Output() closeEvent : EventEmitter<void> = new EventEmitter();

  formStep : VerifyPhoneFormStepEnum = VerifyPhoneFormStepEnum.Verification;

  constructor(
    private _userService : UserService,
    private _authService : AuthService,
    private _modalService : ActionModalService
  ) { 
    this.verifyPhoneForm = new FormGroup({
      code : new FormControl("")
    })
  }

  ngOnInit() {
  }

  getControl(form : FormGroup,name: string): FormControl {
    return form.get(name) as FormControl;
  }

  verifyPhone(){
    this._authService.verifyPhone({email : this.email, token: this.verifyPhoneForm.value.code}).subscribe((data) => {
      this._modalService.popup(ActionModalStatusEnum.Success, "Success ! ", "Phone Verified Successfully",4000);
      this.formStep = VerifyPhoneFormStepEnum.Verified;
    },(error) =>{
      this._modalService.popup(ActionModalStatusEnum.Error,"Error !","Something Went wrong please try again later", 4000);
    })
  }

  closeModal(){
    this.closeEvent.emit();
  }
}
