import { Component, Input, OnInit } from '@angular/core';
import { ChargingSessionService } from 'src/_services/charging-session.service';

@Component({
  selector: 'app-connector-realtime-charging-sessions',
  templateUrl: './connector-realtime-charging-sessions.component.html',
  styleUrls: ['./connector-realtime-charging-sessions.component.sass']
})
export class ConnectorRealtimeChargingSessionsComponent implements OnInit {

  @Input() chargePoint : any = {};
  isHovered : boolean = false;
  chargingSessions : any[] = [];

  constructor(
    private _chargingSessionService : ChargingSessionService
  ) { }

  ngOnInit() {
    this.getChargingSessions();
  }

  // Getting charging sessions for a sepcific chargepoint
  getChargingSessions(){
    this._chargingSessionService.getChargePointChargingSessions(this.chargePoint.id).subscribe((data) => {
      this.chargingSessions = data;
      console.log("this is the charging sessions");
      console.log(this.chargingSessions);
    })
  }

}
