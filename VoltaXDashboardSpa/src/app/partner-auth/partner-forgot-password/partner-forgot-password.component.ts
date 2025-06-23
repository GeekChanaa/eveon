import { Component, OnInit } from '@angular/core';
import { FormGroup, FormControl, Validators } from '@angular/forms';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { AuthService } from 'src/_services/auth.service';
import { PartnerAuthService } from 'src/_services/partner-auth.service';

@Component({
  selector: 'app-partner-forgot-password',
  templateUrl: './partner-forgot-password.component.html',
  styleUrls: ['./partner-forgot-password.component.sass']
})
export class PartnerForgotPasswordComponent implements OnInit {

  email : string = "";
  isLoading : boolean = false;
  form: FormGroup;
  errorMessage : string = "";
  requestMailSent : boolean = false;

  constructor(
    private _authService: AuthService,
    private _partnerAuthService : PartnerAuthService,
    private _modalService: ActionModalService
  ) { 
    this.form = new FormGroup({
      email: new FormControl('', [
        Validators.required,
        Validators.email
      ])
    });
  }

  // On init cycle hook
  ngOnInit() {
  }

  resetPasswordRequest(){
    this.isLoading = true;
    this._partnerAuthService.resetPasswordRequest(this.form.value.email).subscribe(data => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Success,"Mail Sent", "The password request mail has been sent to your email, please check you email", 4000);
      this.requestMailSent = true;
    },(error) => {
      this.isLoading = false;
      this.errorMessage = error.error.error;
      this._modalService.popup(ActionModalStatusEnum.Error,"Error ! ", "Something went wrong, please try again later", 4000);
    });
  }

  getControl(name: string): FormControl {
    return this.form.get(name) as FormControl;
  }
}
