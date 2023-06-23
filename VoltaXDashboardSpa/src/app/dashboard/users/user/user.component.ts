import { Component, OnInit } from '@angular/core';
import { UserService } from 'src/_services/user.service';
import { ActivatedRoute } from '@angular/router';
import { User } from 'src/_models/user';
enum UserTabsEnum {
  InformationsTab = "InformationsTab",
  RechargeCardsTab = "RechargeCardsTab",
  DebitCardsTab = "DebitCardsTab"
}
@Component({
  selector: 'app-user',
  templateUrl: './user.component.html',
  styleUrls: ['./user.component.css']
})
export class UserComponent implements OnInit {

  // TabsEnum
  tabsEnum : UserTabsEnum = UserTabsEnum.InformationsTab;

  //user
  user : User = {
    id: 0,
    firstName: '',
    lastName: '',
    email: '',
    phone: ''
  }

  constructor(
    private _userService: UserService,
    private _route: ActivatedRoute
    ) { }

  ngOnInit() {
    var idParam = this._route.snapshot.paramMap.get('id')
    if (idParam != null){
      this._userService.getById(parseInt(idParam)).subscribe((data)=>{
        this.user = data;
      })
    }
  }

  // Changing current tab
  changeTab(tab : any){
    this.tabsEnum = tab;
  }

}
