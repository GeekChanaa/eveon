import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ChargePointCRListDto } from 'src/_models/_dtos/charge-point-cr-list-dto';
import { ChargePointCategoryEnum } from 'src/_models/_enums/charge-point-category';
import { ChargePointStatusEnum } from 'src/_models/_enums/charge-point-status';
import { ChargePoint } from 'src/_models/charge-point';
import { ChargePointService } from 'src/_services/charge-point.service';

@Component({
  selector: 'app-connector-realtime-list',
  templateUrl: './connector-realtime-list.component.html',
  styleUrls: ['./connector-realtime-list.component.sass']
})
export class ConnectorRealtimeListComponent implements OnInit {

  cities : any[] = [];
  fields: string[] = [];
  filters : any = {
    category:"",
    city : ""
  };
  

  chargePoint: ChargePointCRListDto = {
    chargePointId: '',
    name: '',
    chargingStationName: '',
    serialNumber: '',
    category: '',
    status: '',
    partnerName: ''
  }

  // Constructor
  constructor(
    private _chargePointService: ChargePointService,
    private _router : Router
  ) { }

  ngOnInit() {
    this._getItemFields();
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
  

  resetFilters(){
    this.filters = {
      category:"",
      city : ""
    }
  }

}
