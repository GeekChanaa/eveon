import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { PageState } from 'src/_models/_enums/page-state.enum';
import { PermissionService } from 'src/_services/roles/permission.service';
import { environment } from 'src/environments/environment';


enum PermissionTabsEnum {
  InformationsTab = "InformationsTab",
  RolesTab = "RolesTab"
}


@Component({
  selector: 'app-permission',
  templateUrl: './permission.component.html',
  styleUrls: ['./permission.component.sass']
})
export class PermissionComponent implements OnInit {

  // TabsEnum
  tabsEnum: PermissionTabsEnum = PermissionTabsEnum.InformationsTab;

  PageState = PageState;
  state: PageState = PageState.Loading;

  permissionID: number = 0;
  permissionLoaded: boolean = false;
  parkingTypeValues: { [key: number]: string; } = {};
  permissionStatusValues: { [key: number]: string; } = {};
  permissionCategoryValues: { [key: number]: string; } = {};
  updatepermissionObservable = (id: number, model: any) => this._permissionService.edit(id, model);

  permission: any = {};

  staticUrl: string = environment.apiStaticFilesUrl;

  // Form group
  form: FormGroup;

  constructor(
    private _permissionService: PermissionService,
    private _route: ActivatedRoute,
  ) {
    this.form = new FormGroup({
      name: new FormControl(''),
      description: new FormControl(''),
    })
  }

  ngOnInit() {
    var idParam = this._route.snapshot.paramMap.get('id')
    if (idParam != null) {
      var id = parseInt(idParam);
      this.getPermissionByID(id);
    }
  }

  getPermissionByID(id: number) {
    this.state = PageState.Loading;
    this.permissionID = id;
    this._permissionService.getPermissionByID(id).subscribe((cs) => {
      this.state = PageState.Success;
      this.permission = cs;
      this.permissionLoaded = true;
    })
  }

  changeTab(tab: any) {
    this.tabsEnum = tab;
  }
}
