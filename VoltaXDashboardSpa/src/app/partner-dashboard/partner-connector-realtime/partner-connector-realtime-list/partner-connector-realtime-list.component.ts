import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ChargePointCRListDto } from 'src/_models/_dtos/charge-point-cr-list-dto';
import { AuthService } from 'src/_services/auth.service';
import { CityService } from 'src/_services/city.service';
import { PartnerChargePointsService } from 'src/_services/partner-services/partner-charge-points.service';

@Component({
  selector: 'app-partner-connector-realtime-list',
  templateUrl: './partner-connector-realtime-list.component.html',
  styleUrls: ['./partner-connector-realtime-list.component.sass']
})
export class PartnerConnectorRealtimeListComponent implements OnInit {


    cities : any[] = [];
    fields: string[] = [];
    filters : any = {
      category:"",
      city : ""
    };

    partnerID : number = 0;
    
  
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
      private _chargePointService: PartnerChargePointsService,
      private _router : Router,
      private _cityService: CityService,
      private _authService : AuthService
    ) { }
  
    ngOnInit() {
      var user = this._authService.decodedToken;
      this.partnerID = parseInt(user.partnerID);
      this._getItemFields();
      this.getCities();
      
    }
  
    getChargePointsObservable = (currentPage:  number | undefined, itemsPerPage : number | undefined, itemParams : any) => this._chargePointService.getAllPartnerChargePoints(this.partnerID,currentPage, itemsPerPage, itemParams);
  
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
  
    getCities(){
      this._cityService.getAllMoroccoCityNames().subscribe((data) => {
        this.cities = data;
      })
    }

}
