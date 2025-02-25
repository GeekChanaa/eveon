import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ChargingSessionListDto } from 'src/_models/_dtos/charging-session-list-dto';
import { ChargingSessionService } from 'src/_services/charging-session.service';

@Component({
  selector: 'app-charging-sessions-list',
  templateUrl: './charging-sessions-list.component.html',
  styleUrls: ['./charging-sessions-list.component.sass']
})
export class ChargingSessionsListComponent implements OnInit {

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
    chargingSessionStatus: ''
  }

  // Constructor
  constructor(
    private _chargingSessionService: ChargingSessionService,
    private _router : Router
  ) { }

  ngOnInit() {
    this._getItemFields();
  }

  getChargingSessionsObservable = (currentPage:  number | undefined, itemsPerPage : number | undefined, itemParams : any) => this._chargingSessionService.getAllChargingSessions(currentPage, itemsPerPage, itemParams);
  deleteChargingSessionObservable = (id : number) => this._chargingSessionService.deleteById(id);
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
