import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ChargePointService } from 'src/_services/charge-point.service';
import { SignalRChargerService } from 'src/_services/signalR-charger.service';
import { WebSocketStatusService } from 'src/_services/websocket-status.service';
import { OCPPActions } from 'src/app/ocpp/messages/requests';
@Component({
  selector: 'app-connector-realtime-actions',
  templateUrl: './connector-realtime-actions.component.html',
  styleUrls: ['./connector-realtime-actions.component.sass']
})
export class ConnectorRealtimeActionsComponent implements OnInit {

  status : string = "inactive";
  OCPPActions : any[] = OCPPActions;
  currentOcppAction : any = {};
  requestHandlerModalVisible : boolean = false;
  chargePoint : any = {};

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

  openRequestHanlderModal(ocppAction : any){
    this.requestHandlerModalVisible = true;
    this.currentOcppAction = ocppAction;
  }

  getChargePointByID(id : number){
    this._chargePointService.getChargePointByID(id).subscribe((cp) => {
      this.chargePoint = cp;
      this._wsStatusService.startPolling(this.chargePoint.chargePointId);
      this._wsStatusService.connectionStatus$.subscribe(
        data => {
          this.status = data?.isActive ? 'active' : 'inactive';
        },
        error => console.error('Error receiving status:', error)
      );

      this._signalrChargerService.startConnection(this.chargePoint.chargePointId);
      this._signalrChargerService.addMessageListener();
      
    })
  }

  // Join the group of a specific charger
  joinCharger(): void {
    this._signalrChargerService.joinChargerGroup(this.chargePoint.chargePointId);
  }

  // Send a message to the charger
  sendMessage(): void {
    const message = 'Start charging';
    this._signalrChargerService.sendMessageToCharger(this.chargePoint.chargePointId, message);
  }

}
