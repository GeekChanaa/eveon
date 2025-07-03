import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { EnumMappingService } from 'src/_services/enum-mapping.service';
import { PermissionService } from 'src/_services/roles/permission.service';
import { RoleService } from 'src/_services/roles/role.service';

@Component({
  selector: 'app-create-role',
  templateUrl: './create-role.component.html',
  styleUrls: ['./create-role.component.sass']
})
export class CreateRoleComponent implements OnInit {

  form : FormGroup;
  permissions : any[] = [];
  permissionOptions: any[] = [];

  ngAfterViewInit() {
  }

  ngOnInit() {
    this.getAllPermissions();
  }

  opacity: number = 0;
  activeDiv = 1;


  reportTypes : any = {};
  reportStatuses : any = {};

  isLoading : boolean = false;


  constructor(
    private _enumService : EnumMappingService,
    private _modalService:  ActionModalService,
    private _router : Router,
    private _fb: FormBuilder,
    private _permissionService:  PermissionService,
    private _roleService : RoleService
  ) {
    this.form = this._fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      permissions: [[], [Validators.required, Validators.minLength(1)]]
    });
  }

  getAllPermissions(){
    this._permissionService.getAllPermissions().subscribe((data) => {
      this.permissions = data;
      this.permissionOptions = data.map(permission => ({
        label: permission.name ,
        value: permission.id
      }));
    });
  }


  getFormControl(name: string): FormControl {
    return this.form.get(name) as FormControl;
  }

  
  onSubmit() {
    if (this.form.valid) {
      this.isLoading = true;
      
      // Create the DTO object
      const createRoleDto = {
        name: this.form.get('name')?.value,
        permissions: this.form.get('permissions')?.value || []
      };

      this._roleService.createRole(createRoleDto).subscribe((data) => {
        this._router.navigateByUrl('/dashboard/roles');
      });
    } else {
      // Mark all fields as touched to show validation errors
      this.form.markAllAsTouched();
    }
  }

  

  getControl(name: string): FormControl {
    return this.form.get(name) as FormControl;
  }

}
