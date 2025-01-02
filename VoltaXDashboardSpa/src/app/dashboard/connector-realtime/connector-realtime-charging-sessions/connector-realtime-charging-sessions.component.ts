import { Component, Input, OnInit } from '@angular/core';
import { Pagination } from 'src/_models/pagination';
import { ChargingSessionService } from 'src/_services/charging-session.service';

@Component({
  selector: 'app-connector-realtime-charging-sessions',
  templateUrl: './connector-realtime-charging-sessions.component.html',
  styleUrls: ['./connector-realtime-charging-sessions.component.sass']
})
export class ConnectorRealtimeChargingSessionsComponent implements OnInit {

  @Input() chargePoint : any = {};
  isHovered : boolean = false;
  isStatusHovered : boolean = false;
  chargingSessions : any[] = [];
  isLoading : boolean = true;

  paginationPages: any[] = [];


  pagination: Pagination = {
    currentPage: 0,
    itemsPerPage: 0,
    totalItems: 0,
    totalPages: 0
  }

  itemParams : any = {};

  itemsPerPage: number = 20;
  currentPage: number = 1;

  constructor(
    private _chargingSessionService : ChargingSessionService
  ) { }

  ngOnInit() {
    this.getChargingSessions();
  }

  getChargingSessions(page : number = 1){
    this.isLoading = true;
    
    this._chargingSessionService.getChargePointChargingSessions(this.chargePoint.id,page, this.itemsPerPage, this.itemParams).subscribe((data) => {
      console.log("this is the result ");
      console.log(data);
      this.isLoading = false;
      if(data.result)
        this.chargingSessions = data.result;
      if(data.pagination){
        this.pagination = data.pagination;
      }
    },(error) => {
      this.isLoading = false;
    })
  }

}
