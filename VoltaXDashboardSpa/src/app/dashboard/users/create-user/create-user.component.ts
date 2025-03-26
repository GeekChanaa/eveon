import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import {  Router } from '@angular/router';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { PartnerService } from 'src/_services/partner.service';
import { UserService } from 'src/_services/user.service';

@Component({
  selector: 'app-create-user',
  templateUrl: './create-user.component.html',
  styleUrls: ['./create-user.component.sass']
})
export class CreateUserComponent implements OnInit {

  userForm : FormGroup;
  isLoading : boolean = false;
  partnersOptions : any[] = [];
  userRoles = [
    { label: "Admin", value: "Admin" },
    { label: "Customer", value: "Customer" },
    { label: "Premium Customer", value: "PremiumCustomer" },
    { label: "Support", value: "Support" }
  ];

  genders = [
    { label: "None", value: "" },
    { label: "Male", value: "Male" },
    { label: "Female", value: "Female" },
  ];

  constructor(
    private _userService : UserService,
    private _partnerService : PartnerService,
    private _modalService : ActionModalService,
    private _router : Router
  ) { 
    this.userForm = new FormGroup({
        firstName : new FormControl('',[Validators.required]),
        lastName : new FormControl('',[Validators.required]),
        email : new FormControl('',[Validators.required, Validators.email]),
        gender : new FormControl(''),
        city : new FormControl(null),
        car : new FormControl(null),
        birthday : new FormControl(null),
        phone : new FormControl('',[Validators.required]),
        password : new FormControl('',[Validators.required]),
        partnerID : new FormControl('',[Validators.required]),
        isEmailVerified : new FormControl(false),
        isPhoneNumberVerified : new FormControl(false),
        role : new FormControl("Customer"),
      })
  }

  ngOnInit() {
    this.getPartners();
  }

  onSubmit(){
    let userToCreate = this.userForm.value;
    this.isLoading = true;
    this._userService.createUserDashboard(userToCreate).subscribe((data) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Success,"User Created !","User Created Successfully !",4000);
      this._router.navigateByUrl("/dashboard/users")
    },(error) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Error,"Error","Something went wrong",4000);
    })
  }

  getControl(name: string): FormControl {
    return this.userForm.get(name) as FormControl;
  }

  getPartners(){
    this._partnerService.getAllPartners(1,-1).subscribe((data) => {
      if(data.result != null)
        this.partnersOptions = data.result.map(partner => ({label : partner.name , value : partner.id}))
    })
  }
}
