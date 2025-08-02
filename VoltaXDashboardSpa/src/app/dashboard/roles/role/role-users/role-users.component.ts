import { Component, Input, OnInit } from '@angular/core';
import { UserService } from 'src/_services/user.service';

@Component({
  selector: 'app-role-users',
  templateUrl: './role-users.component.html',
  styleUrls: ['./role-users.component.sass']
})
export class RoleUsersComponent implements OnInit {

  @Input() roleID : number = 0;
  users : any[] = [];

  constructor(
    private _userService : UserService,
  ) { }

  ngOnInit() {
    this.getRoleUsers();
  }

  getRoleUsers(){
    this._userService.getRoleUsers(this.roleID).subscribe((data) => {
      this.users = data;
    })
  }

}
