import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ChargingSessionListDto } from 'src/_models/_dtos/charging-session-list-dto';
import { AuthService } from 'src/_services/auth.service';
import { ChargingSessionService } from 'src/_services/charging-session.service';

@Component({
  selector: 'app-partner-charging-sessions-list',
  templateUrl: './partner-charging-sessions-list.component.html',
  styleUrls: ['./partner-charging-sessions-list.component.sass']
})
export class PartnerChargingSessionsListComponent implements OnInit {
    fields: string[] = [];
    filters : any = {
      category:"",
      city : ""
    };
  
    chargingSession: ChargingSessionListDto = {
      id: 0,
      connectorID: 0,
      connector: "",
      chargePointID: 0,
      userName: '',
      cardNumber: '',
      startDate: new Date(),
      endDate: new Date(),
      stoppedReason: '',
      chargingSessionStatus: '',
      chargePointName: '',
      chargedMinutes: 0,
      idleMinutes: 0,
      chargedKwhs: 0,
      pricePerMinute: 0,
      costPerKwh: 0,
      pricePerIdleMinute: 0,
      totalPriceWithVAT: 0
    }

    partnerID: number = 0;
  
    // Constructor
    constructor(
      private _chargingSessionService: ChargingSessionService,
      private _authService : AuthService,
      private _router : Router
    ) { }
  
    ngOnInit() {
      this._getItemFields();
      var user = this._authService.decodedToken;
      this.partnerID = parseInt(user.partnerID);
    }
  
    getChargingSessionsObservable = (currentPage:  number | undefined, itemsPerPage : number | undefined, itemParams : any) => this._chargingSessionService.getAllPartnerChargingSessions(this.partnerID,currentPage, itemsPerPage, itemParams);
    updateChargingSessionObservable = (id : number, model : any) => this._chargingSessionService.edit(id, model);
  
    private _getItemFields() {
      if (!this.chargingSession || this.chargingSession == undefined) {
        return;
      }
      Object.keys(this.chargingSession ?? {}).forEach((element: string) => {
        if (typeof this.chargingSession?.[element] == "object" && this.chargingSession?.[element] != null && this.chargingSession?.[element].constructor.name == "Date")
          this.fields.push(element);
        if (typeof this.chargingSession?.[element] != "object") this.fields.push(element);
      });
    }
    
  
    resetFilters(){
      this.filters = {
        category:"",
        city : ""
      }
    }

}
