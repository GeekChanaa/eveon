import { Component, EventEmitter, HostListener, Input, OnInit, Output } from '@angular/core';
import { FormGroup, FormControl, Validators } from '@angular/forms';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { AuthService } from 'src/_services/auth.service';
import { UserService } from 'src/_services/user.service';
import { isValidPhoneNumber } from 'src/app/validators/email-or-phone-validator';

enum UpdatePhoneFormStepEnum{
  UpdatePhone = 0,
  VerificationChoice = 1,
  Verification = 2,
  Verified = 3
}

@Component({
  selector: 'app-profile-update-phone',
  templateUrl: './profile-update-phone.component.html',
  styleUrls: ['./profile-update-phone.component.sass']
})
export class ProfileUpdatePhoneComponent implements OnInit {

  emailToChange : string = "";
  
    updatePhoneForm : FormGroup;
    verifyPhoneForm : FormGroup;
  
    UpdatePhoneFormStepEnum = UpdatePhoneFormStepEnum;
  
    @Input() userID : number = 0;
    @Input() email : string = "";
  
    @Output() closeEvent : EventEmitter<void> = new EventEmitter();
    
  
    formStep : UpdatePhoneFormStepEnum = UpdatePhoneFormStepEnum.UpdatePhone;
  
    constructor(
      private _userService : UserService,
      private _authService : AuthService,
      private _modalService : ActionModalService
    ) { 
      this.updatePhoneForm = new FormGroup({
        phone : new FormControl("", [Validators.required, control => isValidPhoneNumber(control.value) ? null : { invalidPhone: true }])
      });
      this.verifyPhoneForm = new FormGroup({
        code : new FormControl("", Validators.required)
      })
    }
  
    ngOnInit() {
    }
  
    getControl(form : FormGroup,name: string): FormControl {
      return form.get(name) as FormControl;
    }
  
    savePhone(){
      this._userService.updatePhone({id : this.userID, phone: this.updatePhoneForm.value.phone}).subscribe((data) => {
        this._modalService.popup(ActionModalStatusEnum.Success, "Success ! ", "Phone Changed Successfully",4000);
        this.formStep = UpdatePhoneFormStepEnum.VerificationChoice;
      },(error) =>{
        this._modalService.popup(ActionModalStatusEnum.Error,"Error !","Something Went wrong please try again later", 4000);
      })
    }
  
    verifyPhone(){
      this._authService.verifyPhone({email : this.email, token: this.verifyPhoneForm.value.code}).subscribe((data) => {
        this._modalService.popup(ActionModalStatusEnum.Success, "Success ! ", "Phone Verified Successfully",4000);
        this.formStep = UpdatePhoneFormStepEnum.Verified;
      },(error) =>{
        this._modalService.popup(ActionModalStatusEnum.Error,"Error !","Something Went wrong please try again later", 4000);
      })
    }
  
    closeModal(){
      this.closeEvent.emit();
    }

}
