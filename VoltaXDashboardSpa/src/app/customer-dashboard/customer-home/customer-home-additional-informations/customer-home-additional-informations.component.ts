import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { AuthService } from 'src/_services/auth.service';
import { CityService } from 'src/_services/city.service';
import { ElectricVehicleModelService } from 'src/_services/electric-vehicle-model.service';
import { UserService } from 'src/_services/user.service';

@Component({
  selector: 'app-customer-home-additional-informations',
  templateUrl: './customer-home-additional-informations.component.html',
  styleUrls: ['./customer-home-additional-informations.component.sass']
})
export class CustomerHomeAdditionalInformationsComponent implements OnInit {

  form : FormGroup;

  cities : any[] = [];
  carBrands : any[] = [];

  genderOptions : any[] = [
    {value:"male", label:"Male"},
    {value:"female", label:"Female"},
    {value:"prefer-not-to-say", label:"Prefer Not To Say"}
  ]

  constructor(
    private _authService : AuthService,
    private _userService : UserService,
    private _cityService : CityService,
    private _electricVehicleModelService : ElectricVehicleModelService
  ) { 
    this.form = new FormGroup({
      gender : new FormControl('prefer-not-to-say'),
      birthday : new FormControl(''),
      city : new FormControl(''),
      carBrand : new FormControl(''),
      electricVehicleModel : new FormControl('')
    })
  }

  ngOnInit() {
    this.getCities();
    this.getElectricVehicleModels();
  }

  getCities(){
    this._cityService.getAllMoroccoCityNames().subscribe((data) => {
      this.cities = data.map((item : any) => {return {name: item.name, id: item.name}})
    })
  }

  getElectricVehicleModels(){
    this._electricVehicleModelService.getAllElectricVehicleModelsForSelect().subscribe((data) => {
      this.carBrands = data.map((item : any) => {return {name: item.make+ " "+item.model, id: item.id}})
    })
  }

  getControl(name: string): FormControl {
    return this.form.get(name) as FormControl;
  }

  

}
