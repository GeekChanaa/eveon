import { Component, OnInit } from '@angular/core';
import { ChargingStationService } from 'src/_services/charging-station.service';
import { ActivatedRoute } from '@angular/router';
import { ChargingStation } from 'src/_models/charging-station';
import { FormControl, FormGroup } from '@angular/forms';
import { ChargePointService } from 'src/_services/charge-point.service';
import { ChargePointCreateDto } from 'src/_models/_dtos/charge-point-create-dto';

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

  // charging station entity
  chargingStation: ChargingStation = {
    id: 0,
    name: '',
    address: '',
    network: '',
    category: '',
    chargerQuantity: '',
    country: '',
    state: '',
    city: '',
    latitude: '',
    longitude: '',
    organisation: '',
    parkingType: '',
    status: '',
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
    private _route: ActivatedRoute
  ) {
    this.chargePointForm = new FormGroup({
      name : new FormControl(''),
      serialNumber : new FormControl(''),
      make : new FormControl(''),
      status : new FormControl(''),
      comment : new FormControl('')
    })
   }

  ngOnInit() {
    var idParam = this._route.snapshot.paramMap.get('id')
    if (idParam != null) {
      var id = parseInt(idParam);
      this.chargingStationID = id;
      this._chargingStationService.getById(id).subscribe((cs) => {
        this.chargingStation = cs;
      })
    }

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
      name: cpf.name,
      serialNumber: cpf.serialNumber,
      make: cpf.make,
      status: cpf.status,
      comment: cpf.comment,
      username: '',
      password: '',
      clientCertThumb: '',
      chargePointId: '',
      chargingStationID: this.chargingStationID
    }
    this._chargePointService.create(chargePoint).subscribe((data) => {
      console.log(data);
    })
  }

  getControl(name: string): FormControl {
    return this.chargePointForm.get(name) as FormControl;
  }

}
