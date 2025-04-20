import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ChargingStationTypeEnum } from '../create-charging-station.component';

@Component({
  selector: 'app-create-charging-station-type-choice',
  templateUrl: './create-charging-station-type-choice.component.html',
  styleUrls: ['./create-charging-station-type-choice.component.sass']
})
export class CreateChargingStationTypeChoiceComponent implements OnInit {

  @Output() choiceEvent : EventEmitter<ChargingStationTypeEnum> = new EventEmitter<ChargingStationTypeEnum>();
  ChargingStationTypeEnum = ChargingStationTypeEnum;
  
  constructor() { }

  ngOnInit() { 
  }

  chooseStation(stationType : ChargingStationTypeEnum){
    this.choiceEvent.emit(stationType);
  }

}
