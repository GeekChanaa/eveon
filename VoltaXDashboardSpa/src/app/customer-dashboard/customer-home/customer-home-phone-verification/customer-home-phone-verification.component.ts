import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ActionModalService } from 'src/_services/action-modal.service';
import { AuthService } from 'src/_services/auth.service';
import { UserService } from 'src/_services/user.service';

enum PhoneStepsEnum{
    EnterPhone = 0,
    VerifyPhone = 1
} 

@Component({
  selector: 'app-customer-home-phone-verification',
  templateUrl: './customer-home-phone-verification.component.html',
  styleUrls: ['./customer-home-phone-verification.component.sass']
})
export class CustomerHomePhoneVerificationComponent implements OnInit {

  @Output() nextStep : EventEmitter<void> = new EventEmitter<void>();
  @Input() userID : number = 0;
  @Input() email : string = "";
  isSendingVerificationSms : boolean = false;
  verificationCode: string = "";
  verifyingPhone : boolean = false;
  phoneCodeSent : boolean = false;
  form: FormGroup;
  phoneForm : FormGroup;
  phone : string = ""

  currentStep  : PhoneStepsEnum = PhoneStepsEnum.EnterPhone;

  PhoneStepsEnum = PhoneStepsEnum;

  constructor(
    private _userService : UserService,
    private _authService: AuthService,
    private _modalService : ActionModalService
  ) {
    this.form = new FormGroup({
      verificationCode: new FormControl('', Validators.required),
    });
    this.phoneForm = new FormGroup({
      phoneNumber: new FormControl('', [
        Validators.required,
        Validators.pattern('^((\\+91-?)|0)?[0-9]{10}$') 
      ])
    })
  }

  ngOnInit() {
    this.getUserPhoneNumber();
  }

  getUserPhoneNumber(){
    this._userService.getUserPhoneNumber(this.userID).subscribe((data) =>{
      if(data != null && data != "" && data != undefined) this.currentStep = PhoneStepsEnum.VerifyPhone;
      this.phone = data;
    });
  }

  getControl(name: string): FormControl {
    return this.form.get(name) as FormControl;
  }

  getPhoneControl(name: string): FormControl {
    return this.phoneForm.get(name) as FormControl;
  }

  sendVerificationSMS(){
    this.isSendingVerificationSms = true;
    this._authService.sendPhoneVerificationSms({phone : this.phone, email : this.email}).subscribe((data) => {
      this.phoneCodeSent = true;
    })
  }

  addPhoneNumber(){
    this.isSendingVerificationSms = true;
    this._userService.updatePhone({id:this.userID, phone: this.phoneForm.value.phoneNumber}).subscribe((data) => {
      this.currentStep = PhoneStepsEnum.VerifyPhone;
      this.phoneCodeSent = true;
      this.isSendingVerificationSms = false;
    })
  }

  verifyPhone(){
    this.verifyingPhone = true;
    this._authService.verifyPhone({email : this.email, token: this.form.value.verificationCode}).subscribe((data) => {
      this.verifyingPhone = false;
      this.nextStep.emit();
    })
  }
}
