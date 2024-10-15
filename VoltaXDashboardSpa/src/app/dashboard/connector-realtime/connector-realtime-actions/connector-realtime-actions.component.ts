import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ChargePointService } from 'src/_services/charge-point.service';
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
    private _chargePointService: ChargePointService
    ) { }

  ngOnInit() {
    var idParam = this._route.snapshot.paramMap.get('id')
    if (idParam != null) {
      var id = parseInt(idParam);
      this.getChargePointByID(id);
    }
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

  getChargePointByID(id : number){
    this._chargePointService.getChargePointByID(id).subscribe((data) => {
      this.chargePoint = data;
      console.log("this is the chargepoint");
      console.log(this.chargePoint);
    })
  }

}
