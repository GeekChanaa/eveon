import { Component, OnInit } from '@angular/core';
import { FormGroup, FormControl } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { ChargePointCategoryEnum } from 'src/_models/_enums/charge-point-category';
import { ChargePointStatusEnum } from 'src/_models/_enums/charge-point-status';
import { ChargePoint } from 'src/_models/charge-point';
import { ChargePointBrandService } from 'src/_services/charge-point-brand.service';
import { ChargePointModelService } from 'src/_services/charge-point-model.service';
import { ChargePointService } from 'src/_services/charge-point.service';
import { ChargingStationService } from 'src/_services/charging-station.service';
import { ConnectorService } from 'src/_services/connector.service';
import { EnumMappingService } from 'src/_services/enum-mapping.service';
import { environment } from 'src/environments/environment';

enum ChargePointTabsEnum {
  InformationsTab = "InformationsTab",
  ConnectorsTab = "ConnectorsTab"
}

@Component({
  selector: 'app-charging-point',
  templateUrl: './charging-point.component.html',
  styleUrls: ['./charging-point.component.sass']
})
export class ChargingPointComponent implements OnInit {

  tabsEnum : ChargePointTabsEnum = ChargePointTabsEnum.InformationsTab;

  chargePointID : number = 0;
  chargePointLoaded : boolean = false;
  parkingTypeValues : { [key: number]: string; } = {};
  chargingStationStatusValues : { [key: number]: string; } = {};
  chargingStationCategoryValues : { [key: number]: string; } = {};
  updateChargePointObservable = (id : number, model : any) => this._chargePointService.edit(id, model);

  chargePointModelsOptions : any = {};
  chargePointBrandsOptions : any = {};

  chargePoint: any = {};

  staticUrl : string = environment.apiStaticFilesUrl;

  // Form group
  chargePointForm : FormGroup;

  constructor(
    private _chargingStationService: ChargingStationService,
    private _chargePointService : ChargePointService,
    private _route: ActivatedRoute,
    private _enumService : EnumMappingService,
    private _chargePointModelService: ChargePointModelService,
    private _chargePointBrandService : ChargePointBrandService
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
      this.getChargePointByID(id);
    }
    this.getAllChargePointModels();
    this.getAllChargePointBrands();
  }


  getChargePointByID(id : number){
    this.chargePointID = id;
    this._chargePointService.getChargePointByID(id).subscribe((cs) => {
      this.chargePoint = cs;
      this.chargePointLoaded = true;
    })
  }

  changeTab(tab : any){
    this.tabsEnum = tab;
  }

  getAllChargePointModels(){
    this._chargePointModelService.getAllChargePointModels().subscribe((data) => {
      this.chargePointModelsOptions = data.map((item : any) => {return {label: item.name, value: item.id}});
    })
  }

  getAllChargePointBrands(){
    this._chargePointBrandService.getAllChargePointBrands().subscribe((data) => {
      this.chargePointBrandsOptions = data.map((item : any) => {return {label: item.name, value: item.id}});
    })
  }

}
