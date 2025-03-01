import { Component, OnInit } from '@angular/core';
import { UserService } from 'src/_services/user.service';
import { ActivatedRoute } from '@angular/router';
import { User } from 'src/_models/user';
import { UserRole } from 'src/_models/_enums/user-role';
enum UserTabsEnum {
  InformationsTab = "InformationsTab",
  RechargeCardsTab = "RechargeCardsTab",
  ChargingSessionsTab = "ChargingSessionsTab",
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
    private _route: ActivatedRoute
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
      console.log("updated")
    })
  }


}
