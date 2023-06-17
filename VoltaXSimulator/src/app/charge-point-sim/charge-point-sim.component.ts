import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MeterValueType } from 'src/_models/MeterValueType';
import { TransactionEventRequest } from 'src/_models/TransactionEventRequest';
import { ChargingStateEnumType } from 'src/_models/_enums/ChargingStateEnumType';
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
  styleUrls: ['./charge-point-sim.component.css']
})
export class ChargePointSimComponent implements OnInit {

  chargePoint: ChargePoint | undefined;
  startTransactionEventRequest: TransactionEventRequest = {
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
      id: 0,
      connectorId : 0
    },
    idToken: {
      idToken: '',
      type: IdTokenEnumType.Central
    }
  };

  stopTransactionEventRequest: TransactionEventRequest = {
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
      id: 0,
      connectorId : 3 
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

  selectedStartTransactionTransactionEventType!: TransactionEventEnumType;
  selectedStartTransactionTriggerReason!: TriggerReasonEnumType;
  selectedStartTransactionReason!: ReasonEnumType;

  selectedStopTransactionTransactionEventType!: TransactionEventEnumType;
  selectedStopTransactionTriggerReason!: TriggerReasonEnumType;
  selectedStopTransactionReason!: ReasonEnumType;

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
  startTransaction(connectorId : any){
    console.log("STARTING THE TRANSACTION");
    console.log(this.startTransactionEventRequest);
    this.startTransactionEventRequest.eventType = this.selectedStartTransactionTransactionEventType;
    this.startTransactionEventRequest.triggerReason = this.selectedStartTransactionTriggerReason;
    this.startTransactionEventRequest.transactionInfo.stoppedReason = this.selectedStartTransactionReason;
    this.startTransactionEventRequest.evse.connectorId = connectorId;
    this._wsService.startTransaction(this.startTransactionEventRequest);
  }

  // Stop transaction 
  stopTransaction(connectorId : any){
    console.log("STOPING THE TRANSACTION");
    console.log(this.stopTransactionEventRequest);
    this.stopTransactionEventRequest.eventType = this.selectedStopTransactionTransactionEventType;
    this.stopTransactionEventRequest.triggerReason = this.selectedStopTransactionTriggerReason;
    this.stopTransactionEventRequest.transactionInfo.stoppedReason = this.selectedStopTransactionReason;
    this.stopTransactionEventRequest.evse.connectorId = connectorId;
    var meterValue : MeterValueType = {
      
      sampledValue: [{
        value : 20,
        context : ReadingContextEnumType.TransactionEnd,
        measurand : MeasurandEnumType.SoC,
        phase : PhaseEnumType.L1,
        location : LocationEnumType.Cable,
        unitOfMeasure : {
          unit : "Wh",
          multiplier : 10
        }
      }],
      timestamp: this._wsService.formatDate(new Date)
    } 
    this.stopTransactionEventRequest.meterValue = [meterValue]
    this._wsService.stopTransaction(this.stopTransactionEventRequest);
  }

}
