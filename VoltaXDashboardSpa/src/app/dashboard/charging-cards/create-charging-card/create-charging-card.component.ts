import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { CardService } from 'src/_services/card.service';
import { UserService } from 'src/_services/user.service';

@Component({
  selector: 'app-create-charging-card',
  templateUrl: './create-charging-card.component.html',
  styleUrls: ['./create-charging-card.component.css']
})
export class CreateChargingCardComponent implements OnInit {

  // FormGroup
  form : FormGroup;

  // Users
  users : any[] = [];

  constructor(
    private _chargingCardService:  CardService,
    private _userService : UserService
  ) { 
    this.form = new FormGroup({
      cardNumber : new FormControl(''),
      account : new FormControl(''),
      cardType : new FormControl(''),
      expirationDate : new FormControl(''),
      maxCount : new FormControl(''),
      status : new FormControl(''),
      balance : new FormControl(''),
      note : new FormControl(''),
      userID : new FormControl('')
    })
  }

  // Getting State Control
  get userControl(): FormControl {
    const control = this.form.get('userID');
    if (!control) {
      throw new Error('User control not found');
    }
    return control as FormControl;
  }

  ngOnInit() {
    this.getAllUserNames();
  }

  onSubmit(){
    console.log(this.form.value);
  }

  // Getting All users
  getAllUserNames(){
    this._userService.getUserNames().subscribe((data) => {
      this.users = data;
    })
  }

  // Getting all user names by name
  getAllUsersNamesByName(name : string){
    this._userService.getAllUsersNamesByName(name).subscribe((data) => {
      this.users = data;
    })
  }

  // User Selected Event
  updateUser(user : any){
    this.userControl.setValue(user.id);
  }
}
