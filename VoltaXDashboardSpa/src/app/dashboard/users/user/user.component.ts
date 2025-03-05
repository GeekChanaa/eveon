import { Component, OnInit } from '@angular/core';
import { UserService } from 'src/_services/user.service';
import { ActivatedRoute } from '@angular/router';
import { User } from 'src/_models/user';
import { UserRole } from 'src/_models/_enums/user-role';
import { ActionModalService } from 'src/_services/action-modal.service';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
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

  // TabsEnum
  tabsEnum : UserTabsEnum = UserTabsEnum.InformationsTab;
  editingSuspension : boolean = false;

  //user
  user : any = {};
  updateUserObservable = (id : number, model : any) => this._userService.editUserDashboardInformations(id, model);

  constructor(
    private _userService: UserService,
    private _route: ActivatedRoute,
    private _modalService: ActionModalService
    ) { }

  ngOnInit() {
    var idParam = this._route.snapshot.paramMap.get('id')
    if (idParam != null){
      this.getUser(parseInt(idParam));
    }
  }

  getUser(id : number){
    this._userService.getUserDashboardDisplayInformations(id).subscribe((data)=>{
      this.user = data;
    })
  }


  // Changing current tab
  changeTab(tab : any){
    this.tabsEnum = tab;
  }

  isSuspended(){ 
    console.log("is suspended : ", this.user.suspendedAt);
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
