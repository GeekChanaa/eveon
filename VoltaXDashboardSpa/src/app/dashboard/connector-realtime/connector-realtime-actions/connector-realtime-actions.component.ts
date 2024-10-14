import { Component, OnInit } from '@angular/core';
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

  constructor(
    private _wsStatusService : WebSocketStatusService
  ) { }

  ngOnInit() {
    const chargePointID = 'VOLTAX02'; 
    this._wsStatusService.startPolling(chargePointID);

    this._wsStatusService.connectionStatus$.subscribe(
      data => {
        this.status = data?.isActive ? 'active' : 'inactive';
      },
      error => console.error('Error receiving status:', error)
    );
  }

  openRequestHanlderModal(ocppAction : any){
    this.requestHandlerModalVisible = true;
    this.currentOcppAction = ocppAction;
  }

}
