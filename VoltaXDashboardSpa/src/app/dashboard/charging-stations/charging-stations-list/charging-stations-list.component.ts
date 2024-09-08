import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ChargingStationCategoryEnum } from 'src/_models/_enums/charging-station-category';
import { ChargingStationStatusEnum } from 'src/_models/_enums/charging-station-status';
import { ParkingTypeEnum } from 'src/_models/_enums/parking-type';
import { ChargingStation } from 'src/_models/charging-station';
import { ChargingStationService } from 'src/_services/charging-station.service';
import { CityService } from 'src/_services/city.service';

@Component({
  selector: 'app-charging-stations-list',
  templateUrl: './charging-stations-list.component.html',
  styleUrls: ['./charging-stations-list.component.sass']
})
export class ChargingStationsListComponent implements OnInit {

  cities : any[] = [];
  fields: string[] = [];
  filters : any = {
    category:"",
    city : ""
  };
  

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
  
  // Getting Morocco Cities: 
  getAllMoroccoCityNames(){
    this._cityService.getAllMoroccoCityNames().subscribe((data) => {
      this.cities = data;
    })
  }

  resetFilters(){
    this.filters = {
      category:"",
      city : ""
    }
  }
  
}
