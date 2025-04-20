import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { CityService } from 'src/_services/city.service';

@Component({
  selector: 'app-create-charging-station-address',
  templateUrl: './create-charging-station-address.component.html',
  styleUrls: ['./create-charging-station-address.component.sass']
})
export class CreateChargingStationAddressComponent implements OnInit {

  @Input() form : FormGroup = new FormGroup({});
  @Output() nextStep : EventEmitter<void> =  new EventEmitter();
  @Output() previousStepEvent : EventEmitter<void> =  new EventEmitter();
  cities : any [] = [];
    
  constructor(
    private _cityService : CityService
  ) { }

  ngOnInit() {
    this.getCities();
  }

  getCities(){
    this._cityService.getAllMoroccoCityNames().subscribe((data) => {
      this.cities = data.map((item : any) => {return {name: item.name, id: item.name}})
    })
  }

  getControl(name: string): FormControl {
    return this.form.get(name) as FormControl;
  }

  saveInformations(){
    this.nextStep.emit();
  }
  
  previousStep = () => this.previousStepEvent.emit();

}
