import { Component, OnInit } from '@angular/core';
import { ChargingStation } from 'src/_models/charging-station';
import { Pagination } from 'src/_models/pagination';
import { ChargingStationService } from 'src/_services/charging-station.service';
import { Router } from '@angular/router';
import { CityService } from 'src/_services/city.service';
import { ParkingTypeEnum } from 'src/_models/_enums/parking-type';
import { ChargingStationStatusEnum } from 'src/_models/_enums/charging-station-status';
import { ChargingStationCategoryEnum } from 'src/_models/_enums/charging-station-category';

@Component({
  selector: 'app-charging-stations',
  templateUrl: './charging-stations.component.html',
  styleUrls: ['./charging-stations.component.sass']
})
export class ChargingStationsComponent implements OnInit {

  cities : any[] = [];
  fields: string[] = [];
  filters : any = {};
  

  chargingStation: ChargingStation = {
    id: 0,
    name: '',
    address: '',
    network: '',
    category: ChargingStationCategoryEnum.Public,
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
  }

  // Constructor
  constructor(
    private _chargingStationService: ChargingStationService,
    private _router : Router,
    private _cityService : CityService
  ) { }

  ngOnInit() {
    this.getAllMoroccoCityNames();
    this._getItemFields();
  }

  getChargingStationsObservable = (currentPage:  number | undefined, itemsPerPage : number | undefined, itemParams : any) => this._chargingStationService.getAll(currentPage, itemsPerPage, itemParams);
  deleteChargingStationObservable = (id : number) => this._chargingStationService.deleteById(id);
  updateChargingStationObservable = (id : number, model : any) => this._chargingStationService.edit(id, model);

  private _getItemFields() {
    if (!this.chargingStation || this.chargingStation == undefined) {
      return;
    }
    Object.keys(this.chargingStation ?? {}).forEach((element: string) => {
      if (typeof this.chargingStation?.[element] == "object" && this.chargingStation?.[element] != null && this.chargingStation?.[element].constructor.name == "Date")
        this.fields.push(element);
      if (typeof this.chargingStation?.[element] != "object") this.fields.push(element);
    });
  }
  
  // delete item
  display(id : number){
    this._router.navigate(['/dashboard/charging-stations',id])
  }

  // Getting Morocco Cities: 
  getAllMoroccoCityNames(){
    this._cityService.getAllMoroccoCityNames().subscribe((data) => {
      this.cities = data;
    })
  }

  

}
