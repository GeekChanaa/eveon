import { Component, Input, OnInit } from '@angular/core';
import { RoleService } from 'src/_services/roles/role.service';

@Component({
  selector: 'app-permission-roles',
  templateUrl: './permission-roles.component.html',
  styleUrls: ['./permission-roles.component.sass']
})
export class PermissionRolesComponent implements OnInit {

  @Input() permissionID : number = 0;
  roles : any[] = [];

  constructor(
    private _roleService: RoleService
  ) { }

  ngOnInit() {
    this.getRoles();
  }

  getRoles(){
    this._roleService.getRolesByPermissionID(this.permissionID).subscribe((data : any) => {
      this.roles = data;
      console.log(this.roles);
    })
  }

}
