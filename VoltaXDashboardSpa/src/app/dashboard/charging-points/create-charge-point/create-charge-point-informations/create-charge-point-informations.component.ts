import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
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
    private _chargingStationService: ChargingStationService,
    private _route : ActivatedRoute
  ) { }

  ngOnInit() {
    this.getChargingStations();
    this._route.queryParams.subscribe(params => {
      if(params['chargingStationID'] != null){
        let chargingStationID = parseInt(params['chargingStationID']);
        this.getControl("chargingStationID").setValue(chargingStationID);
      }
    });
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
