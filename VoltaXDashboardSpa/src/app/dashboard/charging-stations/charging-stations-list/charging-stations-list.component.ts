import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ListFilterDef } from 'src/_models/list-filter';
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

  fields: string[] = [];
  filterDefs: ListFilterDef[] = this._buildFilterDefs([]);
  

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
  
  // The city filter offers Morocco's cities once they arrive.
  getAllMoroccoCityNames(){
    this._cityService.getAllMoroccoCityNames().subscribe({
      next: (cities: { name: string }[]) => this.filterDefs = this._buildFilterDefs((cities ?? []).map(c => c.name)),
      error: () => {}
    });
  }

  /** The city filter exists from the start (a filter remembered from earlier applies on the first load); its choices arrive with the cities. */
  private _buildFilterDefs(cities: string[]): ListFilterDef[] {
    return [
      { key: 'status', label: 'Status', type: 'multiselect', options: [
        { value: 'Available', label: 'Available' },
        { value: 'UnderMaintenance', label: 'Under maintenance' },
        { value: 'Offline', label: 'Offline' },
      ] },
      { key: 'category', label: 'Category', type: 'multiselect', options: ['Public', 'Private', 'Partner'].map(v => ({ value: v, label: v })) },
      { key: 'network', label: 'Network', type: 'multiselect', options: ['Public', 'Private'].map(v => ({ value: v, label: v })) },
      { key: 'city', label: 'City', type: 'multiselect', options: cities.map(name => ({ value: name, label: name })) },
      { key: 'partner', field: 'Partner-Name', label: 'Partner', type: 'text' },
      { key: 'chargerQuantity', label: 'Chargers', type: 'number-range' },
      { key: 'createdAt', label: 'Added', type: 'date-range' },
    ];
  }
}
