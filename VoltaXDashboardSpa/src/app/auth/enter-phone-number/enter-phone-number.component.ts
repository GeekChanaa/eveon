import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute, Router } from '@angular/router';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { AuthService } from 'src/_services/auth.service';
import { UserService } from 'src/_services/user.service';

@Component({
  selector: 'app-enter-phone-number',
  templateUrl: './enter-phone-number.component.html',
  styleUrls: ['./enter-phone-number.component.sass']
})
export class EnterPhoneNumberComponent implements OnInit {

  form : FormGroup;
  email : string = "";
  checkingPhone : boolean = false;
  phoneTimeout : any = {};
  phoneExists : boolean = false;

  isLoading : boolean = false;
  errorMessage : string = "";
  

  // Constructor
  constructor(
    private _authService : AuthService,
    private _router : Router,
    private _modalService : ActionModalService
  ) { 
    this.form = new FormGroup({
      phone: new FormControl('', [
        Validators.required,
        Validators.pattern('^((\\+91-?)|0)?[0-9]{10}$') 
      ])
    })
  }

  // on init cycle hook
  ngOnInit() {
    this.email = this._authService.getAuthInformation().unique_name;
  }

  sendPhoneVerification(){
    this.isLoading = true;
    var addPhoneNumberDto : any = {};
    addPhoneNumberDto.email = this.email;
    addPhoneNumberDto.phone = this.form.value.phone;
    this._authService.sendPhoneVerificationSms(addPhoneNumberDto).subscribe((data) => {
        this._router.navigate(['/auth/verify-phone']);
    } , (error) => {
      this.isLoading = false;
      this.errorMessage = error.error.error;
      this._modalService.popup(ActionModalStatusEnum.Error,"Error ! ", "Something went wrong, please try again later", 4000);
    })
  }

  isPhoneValid(){
    return this.form.valid && !this.phoneExists && !this.checkingPhone
  }

  getControl(name: string): FormControl {
    return this.form.get(name) as FormControl;
  }

}
