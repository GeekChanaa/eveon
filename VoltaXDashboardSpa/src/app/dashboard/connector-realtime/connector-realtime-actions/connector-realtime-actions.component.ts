import { Component, Input, OnInit } from '@angular/core';
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
  
  @Input() chargePoint : any = {};

  currentOcppAction : any = {};
  requestHandlerModalVisible : boolean = false;
  
  ngOnInit() {
  }

  OCPPActions : any[] = OCPPActions;

  openRequestHanlderModal(ocppAction : any){
    this.requestHandlerModalVisible = true;
    this.currentOcppAction = ocppAction;
  }

  

}
