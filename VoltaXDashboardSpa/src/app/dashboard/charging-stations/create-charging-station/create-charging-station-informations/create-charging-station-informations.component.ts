import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { ChargingStationCategoryEnum } from 'src/_models/_enums/charging-station-category';
import { EnumMappingService } from 'src/_services/enum-mapping.service';
import { PartnerService } from 'src/_services/partner.service';
import { ChargingStationTypeEnum } from '../create-charging-station.component';

@Component({
  selector: 'app-create-charging-station-informations',
  templateUrl: './create-charging-station-informations.component.html',
  styleUrls: ['./create-charging-station-informations.component.sass']
})
export class CreateChargingStationInformationsComponent implements OnInit {

  @Input() form : FormGroup = new FormGroup({});
  @Output() nextStep : EventEmitter<void> =  new EventEmitter();
  @Output() previousStepEvent : EventEmitter<void> =  new EventEmitter();
  ChargingStationCategoryEnum = ChargingStationCategoryEnum;
  ChargingStationTypeEnum = ChargingStationTypeEnum;
  @Input() chargingStationType : ChargingStationTypeEnum = ChargingStationTypeEnum.VoltaXStation;
  chargingStationCategories : any[] = []

  partners : any[] = [];

  constructor(
    private _partnerService : PartnerService
  ) { }

  ngOnInit() {
    this.getPartners();
  }

  getPartners(){
    this._partnerService.getAllPartnersForSelect().subscribe((data) => {
      this.partners = data;
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
