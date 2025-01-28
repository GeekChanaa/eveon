import { Component, HostListener, Input, OnInit, Type } from '@angular/core';
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

  requests: string[] = [
    "RequestStartTransaction",
    "GetBaseReport",
    "ChangeAvailabilityRequest",
    "ClearCacheRequest",
    "ClearDisplayMessageRequest",
    "ClearVariableMonitoringRequest",
    "GetDisplayMessagesRequest",
    "GetLogRequest",
    "GetMonitoringReportRequest",
    "GetReportRequest",
    "GetTransactionStatusRequest",
    "GetVariablesRequest",
    "InstallCertificateRequest",
    "ResetRequest",
    "SetDisplayMessageRequest",
    "SetMonitoringBaseRequest",
    "SetMonitoringLevelRequest",
    "SetNetworkProfileRequest",
    "SetVariableMonitoringRequest",
    "SetVariablesRequest",
  ];

  currentOcppAction : any = {};
  requestHandlerModalVisible : string = "";
  
  ngOnInit() {
  }

  OCPPActions : any[] = OCPPActions;

  openRequestHandlerModal(ocppAction : any){
    this.requestHandlerModalVisible = ocppAction;
  }

  closeModal(){
    this.requestHandlerModalVisible = "";
  }


  @HostListener('document:keydown.escape', ['$event'])
  handleEscapeKey(event: KeyboardEvent) {
    this.closeModal();
  }

}
