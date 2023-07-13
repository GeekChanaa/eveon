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

enum ChargingStationTabsEnum {
  InformationsTab = "InformationsTab",
  ChargePointsTab = "ChargePointsTab"
}

@Component({
  selector: 'app-charging-station',
  templateUrl: './charging-station.component.html',
  styleUrls: ['./charging-station.component.css']
})
export class ChargingStationComponent implements OnInit {

  // TabsEnum
  tabsEnum : ChargingStationTabsEnum = ChargingStationTabsEnum.InformationsTab;

  chargingStationID : number = 0;
  cpfShow : Boolean = false;
  chargingStationLoaded : boolean = false;
  parkingTypeValues : { [key: number]: string; } = {};
  chargingStationStatusValues : { [key: number]: string; } = {};
  chargingStationCategoryValues : { [key: number]: string; } = {};

  // charging station entity
  chargingStation: ChargingStation = {
    id: 0,
    name: '',
    address: '',
    network: '',
    category: ChargingStationCategoryEnum.Partner,
    chargerQuantity: '',
    country: '',
    state: '',
    city: '',
    latitude: '',
    longitude: '',
    organisation: '',
    parkingType: ParkingTypeEnum.AngleParking,
    status: ChargingStationStatusEnum.Available,
    wifiAmenity: '',
    parkingAmenity: '',
    restaurantsAmenity: '',
    washroomAmenity: '',
    sittingAreaAmenity: '',
    chargePoints: []
  };

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

  // populating options for the selectboxes : 
  populatingSelectBoxes(){
    this._enumService.getEnumMapping("ChargingStationCategoryEnum");
  }

  // get charging Station by id
  getChargingStationByID(id : number){
    this.chargingStationID = id;
      this._chargingStationService.getById(id).subscribe((cs) => {
        this.chargingStation = cs;
        this.chargingStationLoaded = true;
        this.cpfShow=false;
      })
  }

  // Changing current tab
  changeTab(tab : any){
    this.tabsEnum = tab;
  }

  // Charge Point Form
  showChargePointForm(){
    this.cpfShow = true;
  }

  cpfOnSubmit(){
    var cpf = this.chargePointForm.value;
    const chargePoint : ChargePointCreateDto = {
      name: "",
      serialNumber: cpf.serialNumber,
      category : parseInt(cpf.category),
      make: cpf.make,
      status: parseInt(cpf.status),
      comment: cpf.comment,
      username: '',
      password: '',
      clientCertThumb: '',
      chargePointId: '',
      chargingStationID: this.chargingStationID
    }
    this._chargePointService.create(chargePoint).subscribe((data) => {
      this.getChargingStationByID(this.chargingStation.id);
      
    })
  }

  getControl(name: string): FormControl {
    return this.chargePointForm.get(name) as FormControl;
  }

  update(vale : any,name : string){
    this.chargingStation[name] = vale;
    this.chargingStation.chargePoints=[];
    this._chargingStationService.edit(this.chargingStation.id, this.chargingStation).subscribe((data) => {
      this.getChargingStationByID(this.chargingStation.id);
    })
  }

  deleteChargePoint(id : number){
    this._chargePointService.deleteById(id).subscribe((data) => {
      this.getChargingStationByID(this.chargingStation.id);
    });
  }

}
