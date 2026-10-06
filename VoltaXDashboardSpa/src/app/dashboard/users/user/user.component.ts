import { AccessService } from 'src/_services/access.service';
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
  AccessTab = "AccessTab",
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
  readonly tabs = [
    { id: UserTabsEnum.InformationsTab, label: 'Information', description: 'Profile & account details', icon: 'M12 11v6M12 7v1M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0' },
    { id: UserTabsEnum.RechargeCardsTab, label: 'Recharge cards', description: 'Cards assigned to this user', icon: 'M4 6h16v12H4zM4 10h16M7 15h3' },
    { id: UserTabsEnum.ChargingSessionsTab, label: 'Charging sessions', description: 'Charging activity', icon: 'M8 3v5m8-5v5M6 8h12v4a6 6 0 0 1-12 0V8Zm6 10v4' },
    { id: UserTabsEnum.OrdersTab, label: 'Orders', description: 'Order history', icon: 'M6 3h12l1 5H5l1-5Zm-1 5h14l-1 13H6L5 8Zm5 4v5m4-5v5' },
    { id: UserTabsEnum.ActionsTab, label: 'Actions', description: 'Account management', icon: 'M12 3v9m0 0 4-4m-4 4-4-4M5 17v3h14v-3' },
    { id: UserTabsEnum.AccessTab, label: 'Roles & permissions', description: 'Account access', icon: 'M12 14a4 4 0 1 0 0-8 4 4 0 0 0 0 8Zm-7 7a7 7 0 0 1 14 0' }
  ];

  //user
  user : any = {};
  updateUserObservable = (id : number, model : any) => this._userService.editUserDashboardInformations(id, model);

  constructor(
    public access: AccessService,
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
    if (this.access.isAdmin) this.getRoles();
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
      this.partnersOptions.unshift({ label: 'Select Partner', value: "" });
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

  visibleTabs() {
    return this.tabs.filter(tab =>
      (tab.id !== UserTabsEnum.AccessTab || this.access.isAdmin) &&
      (tab.id !== UserTabsEnum.ActionsTab || this.access.can('EditUsers'))
    );
  }

  onTabKey(event: KeyboardEvent, index: number): void {
    const tabs = this.visibleTabs();
    let next = index;
    if (event.key === 'ArrowRight') next = (index + 1) % tabs.length;
    else if (event.key === 'ArrowLeft') next = (index + tabs.length - 1) % tabs.length;
    else if (event.key === 'Home') next = 0;
    else if (event.key === 'End') next = tabs.length - 1;
    else return;
    event.preventDefault();
    this.changeTab(tabs[next].id);
    const tablist = (event.currentTarget as HTMLElement).parentElement;
    (tablist?.querySelectorAll('button')[next] as HTMLButtonElement)?.focus();
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
