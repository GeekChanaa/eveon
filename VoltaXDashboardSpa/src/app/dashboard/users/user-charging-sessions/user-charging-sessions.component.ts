import { Component, Input, OnInit } from '@angular/core';
import { ChargingSessionListDto } from 'src/_models/_dtos/charging-session-list-dto';
import { ChargingSessionService } from 'src/_services/charging-session.service';

@Component({
  selector: 'app-user-charging-sessions',
  templateUrl: './user-charging-sessions.component.html',
  styleUrls: ['./user-charging-sessions.component.sass']
})
export class UserChargingSessionsComponent implements OnInit {

  @Input() userID : number = 0;
  
  constructor(
    private _chargingSessionService : ChargingSessionService
  ) { }

  fields: string[] = [];
  filters : any = {
    role:""
  };
  

  chargingSession: ChargingSessionListDto = {
    id: 0,
    connectorID: 0,
    connector: '',
    chargePointID: 0,
    userName: '',
    cardNumber: '',
    startDate: new Date(),
    endDate: new Date(),
    stoppedReason: '',
    chargingSessionStatus: ''
  }


  ngOnInit() {
    this._getItemFields();
  }

  getUserChargingSessionsObservable = (currentPage:  number | undefined, itemsPerPage : number | undefined, itemParams : any) => this._chargingSessionService.getUserChargingSessions(this.userID, currentPage ?? 0, itemsPerPage ?? -1, itemParams);

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
      role:""
    }
  }
}
