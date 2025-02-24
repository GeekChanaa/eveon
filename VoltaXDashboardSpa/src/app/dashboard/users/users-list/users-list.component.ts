import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { UserListDto } from 'src/_models/_dtos/user-list-dto';
import { UserService } from 'src/_services/user.service';

@Component({
  selector: 'app-users-list',
  templateUrl: './users-list.component.html',
  styleUrls: ['./users-list.component.sass']
})
export class UsersListComponent implements OnInit {

  fields: string[] = [];
  filters : any = {
    category:"",
    city : ""
  };
  

  user: UserListDto = {
    id: 0,
    firstName: '',
    lastName: '',
    email: '',
    gender: '',
    city: '',
    car: '',
    birthday: new Date(),
    phone: '',
    partnerName: '',
    isEmailVerified: false,
    isPhoneNumberVerified: false,
    role: '',
    suspendedAt: '',
    fullName: ''
  }

  // Constructor
  constructor(
    private _userService: UserService,
    private _router : Router
  ) { }

  ngOnInit() {
    this._getItemFields();
  }

  getUsersObservable = (currentPage:  number | undefined, itemsPerPage : number | undefined, itemParams : any) => this._userService.getAllUsers(currentPage, itemsPerPage, itemParams);
  deleteUserObservable = (id : number) => this._userService.deleteById(id);
  updateUserObservable = (id : number, model : any) => this._userService.edit(id, model);

  private _getItemFields() {
    if (!this.user || this.user == undefined) {
      return;
    }
    Object.keys(this.user ?? {}).forEach((element: string) => {
      if (typeof this.user?.[element] == "object" && this.user?.[element] != null && this.user?.[element].constructor.name == "Date")
        this.fields.push(element);
      if (typeof this.user?.[element] != "object") this.fields.push(element);
    });
  }
  

  resetFilters(){
    this.filters = {
      category:"",
      city : ""
    }
  }
}
