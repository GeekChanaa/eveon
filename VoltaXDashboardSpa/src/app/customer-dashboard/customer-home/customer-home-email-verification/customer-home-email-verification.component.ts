import { Component, Input, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { AuthService } from 'src/_services/auth.service';
import { UserService } from 'src/_services/user.service';

@Component({
  selector: 'app-customer-home-email-verification',
  templateUrl: './customer-home-email-verification.component.html',
  styleUrls: ['./customer-home-email-verification.component.sass']
})
export class CustomerHomeEmailVerificationComponent implements OnInit {

  isSendingVerificationEmail : boolean = false;
  userID: number = 0;
  email : string = "";
  emailCodeSent : boolean = false;
  verifyingEmail : boolean = false;
  verificationCode : string = "";
  form : FormGroup;

  constructor(
    private _userService : UserService,
    private _authService: AuthService,
    private _modalService : ActionModalService
  ) { 
    this.form = new FormGroup({
      verificationCode : new FormControl('', Validators.required),
    })
  }

  ngOnInit() {
    var user = this._authService.getAuthInformation();
    this.userID = parseInt(user.nameid);
    this.email = user.unique_name;
  }

  sendVerificationEmail(){
    this.isSendingVerificationEmail = true;
    this._authService.sendEmailVerificationCode(this.userID).subscribe((data) => {
      this.emailCodeSent = true;
      this._modalService.popup(ActionModalStatusEnum.Success,"Succcess !","Verification Email Sent successfully !",4000);
      this.isSendingVerificationEmail = false
    },(error) => {
      this.emailCodeSent = true;
      this._modalService.popup(ActionModalStatusEnum.Error,"Error","Something went wrong!",4000);
    })
  }

  verifyEmail(){
    this.verifyingEmail = true;
    this._authService.verifyEmail({email : this.email, token : this.form.value.verificationCode}).subscribe((data) => {
      this.verifyingEmail = false
      this._modalService.popup(ActionModalStatusEnum.Success,"Succcess !","Email verified successfully !",4000);
    },(error) => {
      this.verifyingEmail = false
      this._modalService.popup(ActionModalStatusEnum.Error,"Error","Something went wrong!",4000);
    })
  }

   getControl(name: string): FormControl {
    return this.form.get(name) as FormControl;
  }

}
