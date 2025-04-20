import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ChargePointCategoryEnum } from 'src/_models/_enums/charge-point-category';
import { ChargePointStatusEnum } from 'src/_models/_enums/charge-point-status';
import { ChargePoint } from 'src/_models/charge-point';
import { ChargePointService } from 'src/_services/charge-point.service';

@Component({
  selector: 'app-charge-points-list',
  templateUrl: './charge-points-list.component.html',
  styleUrls: ['./charge-points-list.component.sass']
})
export class ChargePointsListComponent implements OnInit {

  cities : any[] = [];
  fields: string[] = [];
  filters : any = {
    category:"",
    city : ""
  };
  

  chargePoint: ChargePoint = {
    id: 0,
    chargePointId: '',
    chargingStationID: 0,
    name: '',
    serialNumber: '',
    make: '',
    category: ChargePointCategoryEnum.Single,
    status: ChargePointStatusEnum.Available,
    comment: '',
    username: '',
    password: '',
    clientCertThumb: '',
    connectors: [],
    transactions: []
  }

  // Constructor
  constructor(
    private _chargePointService: ChargePointService,
    private _router : Router
  ) { }

  ngOnInit() {
    this._getItemFields();
  }

  getChargePointsObservable = (currentPage:  number | undefined, itemsPerPage : number | undefined, itemParams : any) => this._chargePointService.getAll(currentPage, itemsPerPage, itemParams);
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
