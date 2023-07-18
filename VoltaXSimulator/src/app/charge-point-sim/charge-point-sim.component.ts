import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MeterValueType } from 'src/_models/MeterValueType';
import { TransactionEventRequest } from 'src/_models/TransactionEventRequest';
import { ChargingStateEnumType } from 'src/_models/_enums/ChargingStateEnumType';
import { ConnectorStatusEnumType } from 'src/_models/_enums/ConnectorStatusEnumType';
import { IdTokenEnumType } from 'src/_models/_enums/IdTokenEnumType';
import { LocationEnumType } from 'src/_models/_enums/LocationEnumType';
import { MeasurandEnumType } from 'src/_models/_enums/MeasurandEnumType';
import { PhaseEnumType } from 'src/_models/_enums/PhaseEnumType';
import { ReadingContextEnumType } from 'src/_models/_enums/ReadingContextEnumType';
import { ReasonEnumType } from 'src/_models/_enums/ReasonEnumType';
import { TransactionEventEnumType } from 'src/_models/_enums/TransactionEventEnumType';
import { TriggerReasonEnumType } from 'src/_models/_enums/TriggerReasonEnumType';

import { ChargePoint } from 'src/_models/charge-point';
import { ChargePointService } from 'src/_services/charge-point.service';
import { WebsocketService } from 'src/_services/websocket.service';

@Component({
  selector: 'app-charge-point-sim',
  templateUrl: './charge-point-sim.component.html',
  styleUrls: ['./charge-point-sim.component.css'],
})
export class ChargePointSimComponent implements OnInit {
  // Selected Connector Status
  selectedConnectorStatus: ConnectorStatusEnumType =
    ConnectorStatusEnumType.Available;

  // Getting all possible connector Statuses
  connectorStatuses = Object.values(ConnectorStatusEnumType);

  STR: any = {};

  chargePoint: ChargePoint | undefined;
  connectorIdTransactionUid: string[] = [];
  

  transactionEvents = Object.values(TransactionEventEnumType);
  triggerReasons = Object.values(TriggerReasonEnumType);
  chargingStates = Object.values(ChargingStateEnumType);
  reasons = Object.values(ReasonEnumType);

  selectedStartTransactionTriggerReason!: TriggerReasonEnumType;
  selectedStartTransactionReason!: ReasonEnumType;

  selectedStopTransactionTriggerReason!: TriggerReasonEnumType;
  selectedStopTransactionReason!: ReasonEnumType;

  constructor(
    private route: ActivatedRoute,
    private pointService: ChargePointService,
    private _wsService: WebsocketService
  ) {}

  // On init cycle hook
  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    this.chargePoint = this.pointService
      .getChargePoints()
      .find((point) => point.chargePointId === id);
    console.log(this.chargePoint);
    // Setting the websocket gateway for the chargepoint
    if (this.chargePoint)
      this._wsService.setWsLink(this.chargePoint.chargePointGateway);
  }

  // Connecting the ws :
  wsConnect() {
    this._wsService.wsConnect();
  }

  // Start Transaction
  startTransaction(req : any, connectorID : number){
    this._wsService.startTransaction(req);
    this.connectorIdTransactionUid[connectorID] = req.transactionInfo.transactionId;
    console.log(this.connectorIdTransactionUid);
  }

  // Stop Transaction
  stopTransaction(req : any, connectorID : number){
    this._wsService.stopTransaction(req, req.transactionInfo.transactionID);
  }

  // send authorize accepted
  authAcc() {
    this._wsService.sendAuthorizeResponse();
  }

  
}
