import { Component, EventEmitter, OnInit, Output, Input } from '@angular/core';
import { TransactionEventRequest } from 'src/_models/TransactionEventRequest';
import { IdTokenEnumType } from 'src/_models/_enums/IdTokenEnumType';
import { LocationEnumType } from 'src/_models/_enums/LocationEnumType';
import { MeasurandEnumType } from 'src/_models/_enums/MeasurandEnumType';
import { PhaseEnumType } from 'src/_models/_enums/PhaseEnumType';
import { ReadingContextEnumType } from 'src/_models/_enums/ReadingContextEnumType';
import { TransactionEventEnumType } from 'src/_models/_enums/TransactionEventEnumType';
import { TriggerReasonEnumType } from 'src/_models/_enums/TriggerReasonEnumType';
import { WebsocketService } from 'src/_services/websocket.service';

@Component({
  selector: 'app-stop-transaction-request',
  templateUrl: './stop-transaction-request.component.html',
  styleUrls: ['./stop-transaction-request.component.css']
})
export class StopTransactionRequestComponent implements OnInit {

  triggerReasons : any[] = [];
  IdTokenEnumTypes : any[] = [];

  @Output() StopTransactionEvent : EventEmitter<any> = new EventEmitter<any>();
  @Input() transactionID : string = "";

  request : TransactionEventRequest = {
    eventType: TransactionEventEnumType.Ended,
    meterValue: [{
        timestamp: this._wsService.formatDate(new Date()),
        sampledValue: [{
            value: 90,
            context: ReadingContextEnumType.SamplePeriodic,
            measurand: MeasurandEnumType.EnergyReactiveExportRegister,
            phase: PhaseEnumType.L1,
            location: LocationEnumType.EV,
            unitOfMeasure: {
              unit: "5",
              multiplier : 0
          }
      }]
    }],
    timestamp: '',
    triggerReason: TriggerReasonEnumType.Deauthorized,
    seqNo: 0,
    transactionInfo: {
      transactionId: ''
    },
    evse: {
      id: 0
    },
    idToken: {
      idToken: '',
      type: IdTokenEnumType.NoAuthorization
    }
  }

  constructor(
    private _wsService: WebsocketService
  ) { }

  ngOnInit() {
    this.IdTokenEnumTypes = Object.values(IdTokenEnumType);
    this.triggerReasons = Object.values(TriggerReasonEnumType);
  }

  sendRequest(){
    this.request.timestamp = this._wsService.formatDate(new Date());
    this.request.transactionInfo.transactionId = this.transactionID;
    this.StopTransactionEvent.emit(this.request);
  }

}
