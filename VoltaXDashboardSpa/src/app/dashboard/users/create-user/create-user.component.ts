import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import {  Router } from '@angular/router';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { ElectricVehicleModelService } from 'src/_services/electric-vehicle-model.service';
import { PartnerService } from 'src/_services/partner.service';
import { RoleService } from 'src/_services/roles/role.service';
import { UserService } from 'src/_services/user.service';
import { strongPasswordValidator } from 'src/app/validators/strong-password-validator';

@Component({
  selector: 'app-create-user',
  templateUrl: './create-user.component.html',
  styleUrls: ['./create-user.component.sass']
})
export class CreateUserComponent implements OnInit {

  userForm : FormGroup;
  isLoading : boolean = false;
  partnersOptions : any[] = [{value : null, label : "None"}];
  roles : any[] = [];
  evModels : any[] = [];
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
    private _roleService : RoleService,
    private _router : Router,
    private _evModelService : ElectricVehicleModelService
  ) { 
    this.userForm = new FormGroup({
        firstName : new FormControl('',[Validators.required]),
        lastName : new FormControl('',[Validators.required]),
        email : new FormControl('',[Validators.required, Validators.email]),
        gender : new FormControl(''),
        city : new FormControl(null),
        birthday : new FormControl(null),
        phone : new FormControl('',[Validators.required]),
        password : new FormControl('',[Validators.required,
                    Validators.minLength(8),
                    strongPasswordValidator()]),
        partnerID : new FormControl(null),
        isEmailVerified : new FormControl(false),
        isPhoneNumberVerified : new FormControl(false),
        roleID : new FormControl(null, Validators.required),
        electricVehicleModelID : new FormControl("")
      })
  }

  ngOnInit() {
    this.getPartners();
    this.getAllRoles();
    this.getAllEVModels();
  }

  getAllEVModels(){
    this._evModelService.getAllElectricVehicleModelsForSelect().subscribe((data) => {
      this.evModels = data.map(u => ({id: u.id, name: u.make+" "+u.model}))
    })
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

  getPartners() {
    this._partnerService.getAllPartners(1, -1).subscribe((data) => {
      if (data.result != null) {
        this.partnersOptions = [
          { label: 'None', value: null }, // Add "None" option
          ...data.result.map(partner => ({
            label: partner.name,
            value: partner.id
          }))
        ];
      }
    });
  }

  getAllRoles(){
    this._roleService.getAllRoles().subscribe((data) => {
      this.roles = data.map((role : any) => ({ label : role.name, value : role.id}));
        const customer = data.find((role: any) => role.name === 'Customer');
        this.userForm.get('roleID')?.setValue(customer?.id ?? null);
    })
  }
}
