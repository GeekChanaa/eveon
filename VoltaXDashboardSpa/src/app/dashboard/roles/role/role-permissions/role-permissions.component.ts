import { Component, Input, OnInit } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { PermissionService } from 'src/_services/roles/permission.service';
import { RoleService } from 'src/_services/roles/role.service';

@Component({
  selector: 'app-role-permissions',
  templateUrl: './role-permissions.component.html',
  styleUrls: ['./role-permissions.component.sass']
})
export class RolePermissionsComponent implements OnInit {

  @Input() roleID : number = 0;
  permissions : any[] = [];
  rolePermissions : any[] = [];

  permissionOptions : any[]=[];
  roleForm : FormGroup;
  roleFormLoading : boolean = false;

  updatingPermisions : boolean = false;
  

  constructor(
    private _permissionService : PermissionService,
    private _roleService : RoleService,
    private _fb : FormBuilder
  ) { 
     this.roleForm = this._fb.group({
        name: ['', [Validators.required, Validators.minLength(2)]],
        permissions: [[], [Validators.required, Validators.minLength(1)]]
      });
  }

  ngOnInit() {
    this.getRolePermissions();
    this.getAllPermissions();
  }

  
  getRolePermissions(){
    this._permissionService.getRolePermissions(this.roleID).subscribe((data) =>{
      this.rolePermissions = data;
      this.roleForm.patchValue({
        permissions: data.map((p:any) => p.id)
      });
    })
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

  updatePermissions() {
    this.roleFormLoading = true;
    const selectedPermissions = this.roleForm.get('permissions')?.value; // array of IDs
    const roleId = this.roleID;
    this._roleService.updateRolePermissions(roleId, selectedPermissions)
      .subscribe({
        next: () => {
          this.roleFormLoading = false;
          this.updatingPermisions = false;
          this.getRolePermissions();
        },
        error: (err) => {
          this.roleFormLoading = false;
          console.error(err);
        }
      });
  }

  getControl(name: string): FormControl {
    return this.roleForm.get(name) as FormControl;
  }


}
