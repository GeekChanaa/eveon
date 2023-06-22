import { Component, OnInit } from '@angular/core';
import { ChargingStationService } from 'src/_services/charging-station.service';
import { ActivatedRoute } from '@angular/router';
import { ChargingStation } from 'src/_models/charging-station';

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


  constructor(
    private _chargingStationService: ChargingStationService,
    private _route: ActivatedRoute
  ) { }

  ngOnInit() {
    var idParam = this._route.snapshot.paramMap.get('id')
    if (idParam != null) {
      var id = parseInt(idParam);
      this._chargingStationService.getById(id).subscribe((cs) => {
        this.chargingStation = cs;
      })
    }

  }

  // Changing current tab
  changeTab(tab : any){
    this.tabsEnum = tab;
  }


  // Getting the charging station service


}
