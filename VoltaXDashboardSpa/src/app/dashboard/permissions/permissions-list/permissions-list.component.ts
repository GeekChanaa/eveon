import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { PermissionListDto } from 'src/_models/_dtos/permission-dtos/permission-list-dto';
import { PermissionService } from 'src/_services/roles/permission.service';

@Component({
  selector: 'app-permissions-list',
  templateUrl: './permissions-list.component.html',
  styleUrls: ['./permissions-list.component.sass']
})
export class PermissionsListComponent implements OnInit {


  fields: string[] = [];
  filters : any = {
    type:""
  };
  

  permission: PermissionListDto = {
    id: 0,
    name: '',
    description: ''
  }

  // Constructor
  constructor(
    private _permissionService: PermissionService,
    private _router : Router
  ) { }

  ngOnInit() {
    this._getItemFields();
  }

  getPermissionsObservable = (currentPage:  number | undefined, itemsPerPage : number | undefined, itemParams : any) => this._permissionService.getAll(currentPage, itemsPerPage, itemParams);
  deletePermissionObservable = (id : number) => this._permissionService.deleteById(id);
  updatePermissionObservable = (id : number, model : any) => this._permissionService.edit(id, model);

  private _getItemFields() {
    if (!this.permission || this.permission == undefined) {
      return;
    }
    Object.keys(this.permission ?? {}).forEach((element: string) => {
      if (typeof this.permission?.[element] == "object" && this.permission?.[element] != null && this.permission?.[element].constructor.name == "Date")
        this.fields.push(element);
      if (typeof this.permission?.[element] != "object") this.fields.push(element);
    });
  }
  

  resetFilters(){
    this.filters = {
      type:"",
    }
  }

}
