import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ListFilterDef } from 'src/_models/list-filter';
import { ChargePointCRListDto } from 'src/_models/_dtos/charge-point-cr-list-dto';
import { ChargePointCategoryEnum } from 'src/_models/_enums/charge-point-category';
import { ChargePointStatusEnum } from 'src/_models/_enums/charge-point-status';
import { ChargePoint } from 'src/_models/charge-point';
import { ChargePointService } from 'src/_services/charge-point.service';
import { CityService } from 'src/_services/city.service';

@Component({
  selector: 'app-connector-realtime-list',
  templateUrl: './connector-realtime-list.component.html',
  styleUrls: ['./connector-realtime-list.component.sass']
})
export class ConnectorRealtimeListComponent implements OnInit {

  fields: string[] = [];
  filterDefs: ListFilterDef[] = this._buildFilterDefs([]);
  

  chargePoint: ChargePointCRListDto = {
    chargePointId: '',
    chargingStationName: '',
    serialNumber: '',
    category: '',
    status: '',
    partnerName: ''
  }

  // Constructor
  constructor(
    private _chargePointService: ChargePointService,
    private _router : Router,
    private _cityService: CityService
  ) { }

  ngOnInit() {
    this._getItemFields();
    this.getCities();
  }

  getChargePointsObservable = (currentPage:  number | undefined, itemsPerPage : number | undefined, itemParams : any) => this._chargePointService.getAllChargePoints(currentPage, itemsPerPage, itemParams);
  deleteChargePointObservable = (id : number) => this._chargePointService.deleteById(id);
  updateChargePointObservable = (id : number, model : any) => this._chargePointService.edit(id, model);

  private _getItemFields() {
    if (!this.chargePoint || this.chargePoint == undefined) {
      return;
    }
    Object.keys(this.chargePoint ?? {}).forEach((element: string) => {
      if (typeof this.chargePoint?.[element] == "object" && this.chargePoint?.[element] != null && this.chargePoint?.[element].constructor.name == "Date")
        this.fields.push(element);
      if (typeof this.chargePoint?.[element] != "object") this.fields.push(element);
    });
  }

  // The city filter offers Morocco's cities once they arrive.
  getCities(){
    this._cityService.getAllMoroccoCityNames().subscribe({
      next: (cities: { name: string }[]) => this.filterDefs = this._buildFilterDefs((cities ?? []).map(c => c.name)),
      error: () => {}
    });
  }

  /**
   * The city filter exists from the start (a filter remembered from earlier applies on the first load); its choices arrive with the cities.
   * The charge points list and the OCPP local list share this list's routeName, hence its remembered filters: keep the keys and types in step.
   */
  private _buildFilterDefs(cities: string[]): ListFilterDef[] {
    return [
      { key: 'status', label: 'Status', type: 'multiselect', options: [
        { value: 'Available', label: 'Available' },
        { value: 'Offline', label: 'Offline' },
        { value: 'UnderMaintenance', label: 'Under maintenance' },
      ] },
      { key: 'category', label: 'Category', type: 'multiselect', options: [
        { value: 'Single', label: 'Single' },
        { value: 'Double', label: 'Double' },
      ] },
      { key: 'station', field: 'ChargingStation-Name', label: 'Charging station', type: 'text' },
      { key: 'city', field: 'ChargingStation-City', label: 'City', type: 'multiselect', options: cities.map(name => ({ value: name, label: name })) },
      { key: 'brand', field: 'ChargePointBrand-Name', label: 'Brand', type: 'text' },
      { key: 'firmwareVersion', label: 'Firmware version', type: 'text' },
    ];
  }

}
