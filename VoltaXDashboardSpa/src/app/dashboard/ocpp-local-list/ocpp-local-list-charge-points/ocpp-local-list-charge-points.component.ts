import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ChargePointCRListDto } from 'src/_models/_dtos/charge-point-cr-list-dto';
import { ChargePointService } from 'src/_services/charge-point.service';

@Component({
  selector: 'app-ocpp-local-list-charge-points',
  templateUrl: './ocpp-local-list-charge-points.component.html',
  styleUrls: ['./ocpp-local-list-charge-points.component.sass']
})
export class OcppLocalListChargePointsComponent implements OnInit {

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
