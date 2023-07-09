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
  startTransactionEventRequest: TransactionEventRequest = {
    eventType: TransactionEventEnumType.Ended,
    meterValue: [],
    timestamp: this._wsService.formatDate(new Date()),
    triggerReason: TriggerReasonEnumType.Authorized,
    seqNo: 0,
    transactionInfo: {
      customData: {
        vendorId: '',
      },
      transactionId: '',
      stoppedReason: ReasonEnumType.DeAuthorized,
    },
    evse: {
      id: 0,
      connectorId: 0,
    },
    idToken: {
      idToken: '',
      type: IdTokenEnumType.Central,
    },
  };

  stopTransactionEventRequest: TransactionEventRequest = {
    eventType: TransactionEventEnumType.Ended,
    meterValue: [],
    timestamp: this._wsService.formatDate(new Date()),
    triggerReason: TriggerReasonEnumType.Authorized,
    seqNo: 0,
    transactionInfo: {
      customData: {
        vendorId: '',
      },
      transactionId: '',
      stoppedReason: ReasonEnumType.DeAuthorized,
    },
    evse: {
      id: 0,
      connectorId: 3,
    },
    idToken: {
      idToken: '',
      type: IdTokenEnumType.Central,
    },
  };

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
    let instance: any = {
      eventType: Object.values(TransactionEventEnumType),
      meterValue: [
        {
          customData: {
            vendorId: '',
          },
          sampledValue: [
            {
              customData: {
                vendorId: '',
              },
              value: 0,
              context: Object.values(ReadingContextEnumType),
              measurand: Object.values(MeasurandEnumType),
              phase: Object.values(PhaseEnumType),
              location: Object.values(LocationEnumType),
              signedMeterValue: {
                customData: {
                  vendorId: '',
                },
                signedMeterData: '',
                signingMethod: '',
                encodingMethod: '',
                publicKey: '',
              },
              unitOfMeasure: {
                customData: {
                  vendorId: '',
                },
                unit: '',
                multiplier: 0,
              },
            },
          ],
          timestamp: '',
        },
      ],
      timestamp: '',
      triggerReason: Object.values(TriggerReasonEnumType),
      seqNo: 0,
      transactionInfo: {
        customData: {
          vendorId: '',
        },
        transactionId: '',
        chargingState: Object.values(ChargingStateEnumType),
        timeSpentCharging: 0,
        stoppedReason: Object.values(ReasonEnumType),
        remoteStartId: 0,
      },
      evse: {
        customData: {
          vendorId: '',
        },
        id: 0,
        connectorId: 0,
      },
      idToken: {
        customData: {
          vendorId: '',
        },
        additionalInfo: [
          {
            customData: {
              vendorId: '',
            },
            additionalIdToken: '',
            type: '',
          },
        ],
        idToken: '',
        type: Object.values(IdTokenEnumType),
      },
    };
    console.log("this is the object description");
    console.log(this.describeObject(instance));

    this.STR = this.describeObject(instance)
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
  startTransaction(connectorId: any) {
    console.log('STARTING THE TRANSACTION');
    console.log(this.startTransactionEventRequest);
    var transaction_id = this.guid();
    this.startTransactionEventRequest.eventType =
      TransactionEventEnumType.Started;
    this.startTransactionEventRequest.triggerReason =
      this.selectedStartTransactionTriggerReason;
    this.startTransactionEventRequest.transactionInfo.stoppedReason =
      this.selectedStartTransactionReason;
    this.startTransactionEventRequest.evse.connectorId = connectorId;
    this.startTransactionEventRequest.transactionInfo.transactionId =
      transaction_id;
    this.connectorIdTransactionUid[connectorId] = transaction_id;
    var meterValue: MeterValueType = {
      sampledValue: [
        {
          value: 0,
          context: ReadingContextEnumType.TransactionEnd,
          measurand: MeasurandEnumType.EnergyActiveImportRegister,
          phase: PhaseEnumType.L1,
          location: LocationEnumType.Cable,
          unitOfMeasure: {
            unit: 'Wh',
            multiplier: 0,
          },
        },
      ],
      timestamp: this._wsService.formatDate(new Date()),
    };
    this.startTransactionEventRequest.meterValue = [meterValue];
    this._wsService.startTransaction(this.startTransactionEventRequest);
    console.log('this is the transaction id : ' + transaction_id);
  }

  // Stop transaction
  stopTransaction(connectorId: any) {
    console.log('STOPING THE TRANSACTION');
    var transaction_id = this.connectorIdTransactionUid[connectorId];
    console.log(transaction_id);
    this.stopTransactionEventRequest.eventType = TransactionEventEnumType.Ended;
    this.stopTransactionEventRequest.triggerReason =
      this.selectedStopTransactionTriggerReason;
    this.stopTransactionEventRequest.transactionInfo.stoppedReason =
      this.selectedStopTransactionReason;
    this.stopTransactionEventRequest.evse.connectorId = connectorId;
    this.stopTransactionEventRequest.transactionInfo.transactionId =
      transaction_id;
    var meterValue: MeterValueType = {
      sampledValue: [
        {
          value: 20,
          context: ReadingContextEnumType.TransactionEnd,
          measurand: MeasurandEnumType.EnergyActiveImportRegister,
          phase: PhaseEnumType.L1,
          location: LocationEnumType.Cable,
          unitOfMeasure: {
            unit: 'Wh',
            multiplier: 0,
          },
        },
      ],
      timestamp: this._wsService.formatDate(new Date()),
    };
    this.stopTransactionEventRequest.meterValue = [meterValue];
    this._wsService.stopTransaction(this.stopTransactionEventRequest);
  }

  // Generating transaction IDS
  guid() {
    function s4() {
      return Math.floor((1 + Math.random()) * 0x10000)
        .toString(16)
        .substring(1);
    }
    return (
      s4() +
      s4() +
      '-' +
      s4() +
      '-' +
      s4() +
      '-' +
      s4() +
      '-' +
      s4() +
      s4() +
      s4()
    );
  }

  // Status notification Request
  statusNotificationRequest(connectorId: number) {
    if (this.chargePoint?.chargePointId != null)
      this._wsService.statusNotification(
        this.selectedConnectorStatus,
        this.chargePoint?.id,
        connectorId
      );
  }

  // send authorize accepted
  authAcc() {
    this._wsService.sendAuthorizeResponse();
  }

  describeObject(obj: any) {
    let description: any = {};

    for (let key in obj) {
      let value = obj[key];
      let valueType = typeof value;

      if (Array.isArray(value)) {
        if (value.length > 0) {
          if(typeof value[0] ==='object'){
            description[key] = {
              name: key,
              type: 'array',
              value: this.describeObject(value[0]),
            };
          }
          else{
            description[key] = {
              name: key,
              type: 'enum',
              value: value,
            };
          }
          
        }
      } else if (valueType === 'object') {
        description[key] = {
          name: key,
          type: 'object',
          value: this.describeObject(value),
        };
      } else if (valueType === 'boolean') {
        description[key] = {
          name: key,
          type: 'bool',
          value: value,
        };
      } else if (valueType === 'number') {
        description[key] = {
          name: key,
          type: 'number',
          value: value,
        };
      } else if (valueType === 'string') {
        description[key] = {
          name: key,
          type: 'string',
          value: value,
        };
      }
    }

    return description;
  }
}
