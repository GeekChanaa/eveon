import { Component, Input, OnInit } from '@angular/core';
import { FormGroup, FormControl } from '@angular/forms';
import { ChargingStationTypeEnum } from 'src/app/dashboard/charging-stations/create-charging-station/create-charging-station.component';

@Component({
  selector: 'app-create-charge-point-add-connector-modal',
  templateUrl: './create-charge-point-add-connector-modal.component.html',
  styleUrls: ['./create-charge-point-add-connector-modal.component.sass']
})
export class CreateChargePointAddConnectorModalComponent implements OnInit {

  @Input() form : FormGroup = new FormGroup({});
  @Input() chargingStationType : ChargingStationTypeEnum | undefined; 

  ChargingStationTypeEnum = ChargingStationTypeEnum;

  powerOptions : any[] = [
    {value:7.3, label:"7.3kw"},
    {value:11, label:"11kw"},
    {value:22, label:"22kw"},
    {value:60, label:"60kw"},
    {value:150, label:"150kw"},
    {value:300, label:"300kw"}
  ]

  connectorTypeOptions: any[] = [
    { value: 'cCCS1', label: 'cCCS1' },
    { value: 'cCCS2', label: 'cCCS2' },
    { value: 'cG105', label: 'cG105' },
    { value: 'cTesla', label: 'cTesla' },
    { value: 'cType1', label: 'cType1' },
    { value: 'cType2', label: 'cType2' },
    { value: 's309-1P-16A', label: 's309-1P-16A' },
    { value: 's309-1P-32A', label: 's309-1P-32A' },
    { value: 's309-3P-16A', label: 's309-3P-16A' },
    { value: 's309-3P-32A', label: 's309-3P-32A' },
    { value: 'sBS1361', label: 'sBS1361' },
    { value: 'sCEE-7-7', label: 'sCEE-7-7' },
    { value: 'sType2', label: 'sType2' },
    { value: 'sType3', label: 'sType3' },
    { value: 'Other1PhMax16A', label: 'Other1PhMax16A' },
    { value: 'Other1PhOver16A', label: 'Other1PhOver16A' },
    { value: 'Other3Ph', label: 'Other3Ph' },
    { value: 'Pan', label: 'Pan' },
    { value: 'wInductive', label: 'wInductive' },
    { value: 'wResonant', label: 'wResonant' },
    { value: 'Undetermined', label: 'Undetermined' },
    { value: 'Unknown', label: 'Unknown' },
  ];

  constructor() { }

  ngOnInit() {
  }

  getControl(name: string): FormControl {
    return this.form.get(name) as FormControl;
  }

}
