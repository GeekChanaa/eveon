import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { TransactionEventRequest } from 'src/_models/TransactionEventRequest';
import { ChargingStateEnumType } from 'src/_models/_enums/ChargingStateEnumType';
import { IdTokenEnumType } from 'src/_models/_enums/IdTokenEnumType';
import { ReasonEnumType } from 'src/_models/_enums/ReasonEnumType';
import { TransactionEventEnumType } from 'src/_models/_enums/TransactionEventEnumType';
import { TriggerReasonEnumType } from 'src/_models/_enums/TriggerReasonEnumType';
import { ChargePoint } from 'src/_models/charge-point';
import { ChargePointService } from 'src/_services/charge-point.service';
import { WebsocketService } from 'src/_services/websocket.service';

@Component({
  selector: 'app-charge-point-sim',
  templateUrl: './charge-point-sim.component.html',
  styleUrls: ['./charge-point-sim.component.css']
})
export class ChargePointSimComponent implements OnInit {

  chargePoint: ChargePoint | undefined;
  transactionEventRequest: TransactionEventRequest = {
    eventType: TransactionEventEnumType.Ended,
    meterValue: [],
    timestamp: this._wsService.formatDate(new Date()),
    triggerReason: TriggerReasonEnumType.Authorized,
    seqNo: 0,
    transactionInfo: {
      customData: {
        vendorId: ''
      },
      transactionId: '',
      stoppedReason : ReasonEnumType.DeAuthorized
    },
    evse: {
      id: 0
    },
    idToken: {
      idToken: '',
      type: IdTokenEnumType.Central
    }
  };

  transactionEvents = Object.values(TransactionEventEnumType);
  triggerReasons = Object.values(TriggerReasonEnumType);
  chargingStates = Object.values(ChargingStateEnumType);
  reasons = Object.values(ReasonEnumType);

  selectedTransactionEvent!: TransactionEventEnumType;
  selectedTriggerReason!: TriggerReasonEnumType;
  selectedReason!: ReasonEnumType;

  constructor(
    private route: ActivatedRoute,
    private pointService: ChargePointService,
    private _wsService : WebsocketService
  ) {
    
  }

  // On init cycle hook
  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    this.chargePoint = this.pointService.getChargePoints().find(point => point.chargePointId === id);

    // Setting the websocket gateway for the chargepoint
    if(this.chargePoint)
    this._wsService.setWsLink(this.chargePoint.chargePointGateway)
  }

  // Connecting the ws : 
  wsConnect(){
    this._wsService.wsConnect();
  }

  // Start Transaction
  startTransaction(){
    console.log("STARTING THE TRANSACTION");
    console.log(this.transactionEventRequest);
    this.transactionEventRequest.eventType = this.selectedTransactionEvent;
    this.transactionEventRequest.triggerReason = this.selectedTriggerReason;
    this.transactionEventRequest.transactionInfo.stoppedReason = this.selectedReason;
    this._wsService.startTransaction(this.transactionEventRequest);
  }

}
