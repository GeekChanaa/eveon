import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { RoleListDto } from 'src/_models/_dtos/role-dtos/role-list-dto';
import { RoleService } from 'src/_services/roles/role.service';

@Component({
  selector: 'app-roles-list',
  templateUrl: './roles-list.component.html',
  styleUrls: ['./roles-list.component.sass']
})
export class RolesListComponent implements OnInit {

  fields: string[] = [];
  filters : any = {
    type:""
  };
  

  role: RoleListDto = {
    id: 0,
    name: ''
  }

  // Constructor
  constructor(
    private _roleService: RoleService,
    private _router : Router
  ) { }

  ngOnInit() {
    this._getItemFields();
  }

  getRolesObservable = (currentPage:  number | undefined, itemsPerPage : number | undefined, itemParams : any) => this._roleService.getAll(currentPage, itemsPerPage, itemParams);
  deleteRoleObservable = (id : number) => this._roleService.deleteById(id);
  updateRoleObservable = (id : number, model : any) => this._roleService.edit(id, model);

  private _getItemFields() {
    if (!this.role || this.role == undefined) {
      return;
    }
    Object.keys(this.role ?? {}).forEach((element: string) => {
      if (typeof this.role?.[element] == "object" && this.role?.[element] != null && this.role?.[element].constructor.name == "Date")
        this.fields.push(element);
      if (typeof this.role?.[element] != "object") this.fields.push(element);
    });
  }
  

  resetFilters(){
    this.filters = {
      type:"",
    }
  }
}
