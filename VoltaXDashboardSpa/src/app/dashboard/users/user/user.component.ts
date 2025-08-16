import { Component, OnInit } from '@angular/core';
import { UserService } from 'src/_services/user.service';
import { ActivatedRoute } from '@angular/router';
import { User } from 'src/_models/user';
import { UserRole } from 'src/_models/_enums/user-role';
import { ActionModalService } from 'src/_services/action-modal.service';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { PartnerService } from 'src/_services/partner.service';
import { RoleService } from 'src/_services/roles/role.service';
import { ElectricVehicleModelService } from 'src/_services/electric-vehicle-model.service';
import { PageState } from 'src/_models/_enums/page-state.enum';
enum UserTabsEnum {
  InformationsTab = "InformationsTab",
  RechargeCardsTab = "RechargeCardsTab",
  ChargingSessionsTab = "ChargingSessionsTab",
  OrdersTab = "OrdersTab",
  ActionsTab = "ActionsTab",
}
@Component({
  selector: 'app-user',
  templateUrl: './user.component.html',
  styleUrls: ['./user.component.sass']
})
export class UserComponent implements OnInit {

  PageState = PageState;
  state: PageState = PageState.Loading;
  // TabsEnum
  tabsEnum : UserTabsEnum = UserTabsEnum.InformationsTab;
  editingSuspension : boolean = false;
  userLoaded : boolean = false;
  partnersOptions : any[] = [];
  rolesOptions : any[] = [];
  evModels : any = {};

  //user
  user : any = {};
  updateUserObservable = (id : number, model : any) => this._userService.editUserDashboardInformations(id, model);

  constructor(
    private _userService: UserService,
    private _route: ActivatedRoute,
    private _modalService: ActionModalService,
    private _partnerService : PartnerService,
    private _roleService : RoleService,
    private _evModelService : ElectricVehicleModelService
    ) { }

  ngOnInit() {
    var idParam = this._route.snapshot.paramMap.get('id')
    if (idParam != null){
      this.getUser(parseInt(idParam));
    }
    this.getAllPartners();
    this.getRoles();
    this.getAllEVModels();
  }

  getUser(id : number){
    this.state = PageState.Loading;
    this.userLoaded = false;
    this._userService.getUserDashboardDisplayInformations(id).subscribe((data)=>{
      this.state = PageState.Success;
      this.userLoaded = true;
      this.user = data;
    })
  }

  getAllPartners(){
    this._partnerService.getAllPartners().subscribe((data) =>{
      if(data.result)
        this.partnersOptions = data.result.map((obj) =>({label: obj.name, value: obj.id}))
    })
  }

  getAllEVModels(){
    this._evModelService.getAllElectricVehicleModelsForSelect().subscribe((data) => {
      this.evModels = data.map(u => ({value: u.id, label: u.make+" "+u.model}))
    })
  }

  getRoles(){
    this._roleService.getAllRoles().subscribe((data) => {
      this.rolesOptions = data.map((obj : any) =>({label: obj.name, value: obj.id}))
    })
  }

  // Changing current tab
  changeTab(tab : any){
    this.tabsEnum = tab;
  }

  isSuspended(){ 
    return new Date(this.user.suspendedAt) > new Date();
  }

  suspendUser(){
    this.updateUserObservable(this.user.id, this.user).subscribe((data) => {
      this._modalService.popup(ActionModalStatusEnum.Success,"Success !","Account suspenstion taken into account",4000);
      this.editingSuspension = false;
    }, (error) => {
      this._modalService.popup(ActionModalStatusEnum.Error, "Error !", "Something went wrong!", 4000);
    })
  }


}
