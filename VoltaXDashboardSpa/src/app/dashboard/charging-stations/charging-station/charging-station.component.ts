import { Component, OnInit } from '@angular/core';
import { ChargingStationService } from 'src/_services/charging-station.service';
import { ActivatedRoute } from '@angular/router';
import { ChargingStation } from 'src/_models/charging-station';
import { FormControl, FormGroup } from '@angular/forms';
import { ChargePointService } from 'src/_services/charge-point.service';
import { ChargePointCreateDto } from 'src/_models/_dtos/charge-point-create-dto';
import { ParkingTypeEnum } from 'src/_models/_enums/parking-type';
import { ChargingStationStatusEnum } from 'src/_models/_enums/charging-station-status';
import { ChargingStationCategoryEnum } from 'src/_models/_enums/charging-station-category';
import { EnumMappingService } from 'src/_services/enum-mapping.service';
import  {environment} from 'src/environments/environment';

enum ChargingStationTabsEnum {
  InformationsTab = "InformationsTab",
  ChargePointsTab = "ChargePointsTab"
}

@Component({
  selector: 'app-charging-station',
  templateUrl: './charging-station.component.html',
  styleUrls: ['./charging-station.component.sass']
})
export class ChargingStationComponent implements OnInit {

  // TabsEnum
  tabsEnum : ChargingStationTabsEnum = ChargingStationTabsEnum.InformationsTab;

  chargingStationID : number = 0;
  chargingStationLoaded : boolean = false;
  parkingTypeValues : { [key: number]: string; } = {};
  chargingStationStatusValues : { [key: number]: string; } = {};
  chargingStationCategoryValues : { [key: number]: string; } = {};
  updateChargingStationObservable = (id : number, model : any) => this._chargingStationService.edit(id, model);

  chargingStation: any = {};

  staticUrl : string = environment.apiStaticFilesUrl;

  // Form group
  chargePointForm : FormGroup;

  constructor(
    private _chargingStationService: ChargingStationService,
    private _chargePointService : ChargePointService,
    private _route: ActivatedRoute,
    private _enumService : EnumMappingService
  ) {
    this.chargePointForm = new FormGroup({
      serialNumber : new FormControl(''),
      make : new FormControl(''),
      status : new FormControl(''),
      category : new FormControl(''),
      comment : new FormControl(''),
      chargePointCategory : new FormControl(''),
    })
   }

  ngOnInit() {
    var idParam = this._route.snapshot.paramMap.get('id')
    if (idParam != null) {
      var id = parseInt(idParam);
      this.getChargingStationByID(id);
    }
    this.populatingSelectBoxes();
  }

  populatingSelectBoxes(){
    this._enumService.getEnumMapping("ChargingStationCategoryEnum");
  }

  getChargingStationByID(id : number){
    this.chargingStationID = id;
      this._chargingStationService.getChargingStationByID(id).subscribe((cs) => {
        this.chargingStation = cs;
        this.chargingStationLoaded = true;
      })
  }

  changeTab(tab : any){
    this.tabsEnum = tab;
  }

}
