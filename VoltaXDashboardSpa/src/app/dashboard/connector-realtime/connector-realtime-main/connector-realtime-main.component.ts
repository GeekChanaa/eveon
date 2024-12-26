import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ChargePointService } from 'src/_services/charge-point.service';
import { SignalRChargerService } from 'src/_services/signalR-charger.service';
import { WebSocketStatusService } from 'src/_services/websocket-status.service';

export enum ChargerNavbarEnum{
  Overview = 1,
  PricingAccess = 2,
  DetailsSettings = 3,
  Actions = 4,
  OCPPConfiguration = 5,
  Firmware = 6,
  UptimeReports = 7,
  Logs = 8,
  Ratings = 9,
  ChargingSessions = 10,
  Transactions = 11,
  Configurations = 12
}

@Component({
  selector: 'app-connector-realtime-main',
  templateUrl: './connector-realtime-main.component.html',
  styleUrls: ['./connector-realtime-main.component.sass']
})
export class ConnectorRealtimeMainComponent implements OnInit {

  selectedMenuItem : ChargerNavbarEnum = ChargerNavbarEnum.Overview;

  status : string = "inactive";

  isHovered: boolean = false;
  disabledActions : boolean = true;
  
  chargePoint : any = {};

  selectMenuItem(item: ChargerNavbarEnum): void {
    this.selectedMenuItem = item;
  }

  constructor(
    private _wsStatusService : WebSocketStatusService,
    private _route: ActivatedRoute,
    private _chargePointService: ChargePointService,
    private _signalrChargerService : SignalRChargerService
    ) { }

  ngOnInit() {
    var idParam = this._route.snapshot.paramMap.get('id')
    if (idParam != null) {
      var id = parseInt(idParam);
      this.getChargePointByID(id);
    }
    
  }

  getChargePointByID(id : number){
    this._chargePointService.getChargePointByID(id).subscribe((cp) => {
      this.chargePoint = cp;
      this._wsStatusService.startPolling(this.chargePoint.chargePointId);
      this._wsStatusService.connectionStatus$.subscribe(
        data => {
          this.status = data?.isActive ? 'available' : 'disconnected';
          console.log("this is the disabled Actions");
          console.log(this.status);
          this.disabledActions = this.status == 'available';
        },
        error => console.error('Error receiving status:', error)
      );
      this._signalrChargerService.startConnection(this.chargePoint.chargePointId);
    })
  }

  // Join the group of a specific charger
  joinCharger(): void {
    this._signalrChargerService.joinChargerGroup(this.chargePoint.chargePointId);
  }

  getMenu(stepName: string): ChargerNavbarEnum | undefined {
    return ChargerNavbarEnum[stepName as keyof typeof ChargerNavbarEnum];
  }


}
