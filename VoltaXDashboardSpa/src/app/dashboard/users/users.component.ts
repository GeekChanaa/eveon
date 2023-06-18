import { Component, OnInit } from '@angular/core';
import { User } from 'src/_models/user';
import { UserService } from 'src/_services/user.service';

@Component({
  selector: 'app-users',
  templateUrl: './users.component.html',
  styleUrls: ['./users.component.css']
})
export class UsersComponent implements OnInit {

  
  // Data
  data : any[] = [];

  // Fields
  fields : string[] = [];

  // Page params
  itemsPerPage : number = 20;
  currentPage : number = 1;

  user : User = {
    id: 0,
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
  }

  // Constructor
  constructor(
    private _userService : UserService
  ) { }

  ngOnInit() {
    this._getItemFields();
    this.getAll();
  }

  
  // Getting All Products
  getAll(){
    this._userService.getAll(this.currentPage,this.itemsPerPage).subscribe(data => {
      if (data.result) {
        this.data = data.result;
      }
    })
  }

  // Getting Item Fields
  private _getItemFields(){
    // Ensure this.user is defined
    if (!this.user || this.user == undefined) {
      return;
    }
    // Getting item fields
    Object.keys(this.user ?? {}).forEach((element : string) => {
      console.log(element); 
      if(typeof this.user?.[element] == "object" && this.user?.[element] != null && this.user?.[element].constructor.name == "Date")
      this.fields.push(element);
      if(typeof this.user?.[element] != "object") this.fields.push(element);
    });
  }

  // Deleting the item

  // Next page
  nextPage(){
    this.currentPage++;
    this.getAll();
  }

  // Previous Page
  previousPage(){
    this.currentPage--;
    this.getAll();
  }

}
