import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { EnumMappingService } from 'src/_services/enum-mapping.service';
import { RoleService } from 'src/_services/roles/role.service';
import { environment } from 'src/environments/environment';


enum RoleTabsEnum {
  InformationsTab = "InformationsTab",
  Permissions = "Permissions",
  Users = "Users",
}
@Component({
  selector: 'app-role',
  templateUrl: './role.component.html',
  styleUrls: ['./role.component.sass']
})
export class RoleComponent implements OnInit {

  tabsEnum : RoleTabsEnum = RoleTabsEnum.InformationsTab;
  RoleTabsEnum = RoleTabsEnum;

  roleID : number = 0;
  roleLoaded : boolean = false;
  updateRoleObservable = (id : number, model : any) => this._roleService.edit(id, model);

  role: any = {};

  staticUrl : string = environment.apiStaticFilesUrl;

  // Form group
  roleForm : FormGroup;

  constructor(
    private _roleService: RoleService,
    private _route: ActivatedRoute,
    private _enumService : EnumMappingService
  ) {
    this.roleForm = new FormGroup({
      serialNumber : new FormControl(''),
      make : new FormControl(''),
      status : new FormControl(''),
      category : new FormControl(''),
      comment : new FormControl(''),
      chargePointCategory : new FormControl(''),
    })
   }

  ngOnInit() {
    var idParam = this._route.snapshot.paramMap.get('id')
    if (idParam != null) {
      var id = parseInt(idParam);
      this.getRoleByID(id);
    }
  }


  getRoleByID(id : number){
    this.roleID = id;
    this._roleService.getById(id).subscribe((cs) => {
      this.role = cs;
      console.log("this is the role : ");
      console.log(this.role);
      this.roleLoaded = true;
    })
  }

  changeTab(tab : any){
    this.tabsEnum = tab;
  }
}
