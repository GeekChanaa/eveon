import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { AuthService } from 'src/_services/auth.service';
import { CityService } from 'src/_services/city.service';
import { UserService } from 'src/_services/user.service';

@Component({
  selector: 'app-customer-home-additional-informations',
  templateUrl: './customer-home-additional-informations.component.html',
  styleUrls: ['./customer-home-additional-informations.component.sass']
})
export class CustomerHomeAdditionalInformationsComponent implements OnInit {

  form : FormGroup;

  cities : any[] = [];
  
  genderOptions : any[] = [
    {value:"male", label:"Male"},
    {value:"female", label:"Female"},
    {value:"prefer-not-to-say", label:"Prefer Not To Say"}
  ]

  constructor(
    private _authService : AuthService,
    private _userService : UserService,
    private _cityService : CityService
  ) { 
    this.form = new FormGroup({
      gender : new FormControl(''),
      birthday : new FormControl(''),
      city : new FormControl(''),
      carBrand : new FormControl('')
    })
  }

  ngOnInit() {
    
  }

  getCities(){
    this._cityService.getAllMoroccoCityNames().subscribe((data) => {
      this.cities = data.map((item : any) => {return {name: item.name, id: item.name}})
    })
  }

  getControl(name: string): FormControl {
    return this.form.get(name) as FormControl;
  }

  

}
