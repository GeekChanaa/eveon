import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { ChargingStationService } from 'src/_services/charging-station.service';

@Component({
  selector: 'app-create-charge-point-informations',
  templateUrl: './create-charge-point-informations.component.html',
  styleUrls: ['./create-charge-point-informations.component.sass']
})
export class CreateChargePointInformationsComponent implements OnInit {

  @Output() nextStep : EventEmitter<any[]> = new EventEmitter<any[]>();
  @Input() form : FormGroup = new FormGroup({});
  chargingStationsOptions : any
  isLoaded = false;

  constructor(
    private _chargingStationService: ChargingStationService
  ) { }

  ngOnInit() {
    this.getChargingStations();
  }

  getChargingStations(){
    this._chargingStationService.getChargingStationsForSelect().subscribe((data) =>  {
      this.chargingStationsOptions = data;
      this.isLoaded = true
    })
  }

  saveInformations(){
    this.nextStep.emit();
  }

  getControl(name: string): FormControl {
    return this.form.get(name) as FormControl;
  }

}
