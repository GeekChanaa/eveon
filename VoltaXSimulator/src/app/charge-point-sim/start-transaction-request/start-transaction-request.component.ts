import { Component, EventEmitter, OnInit , Output} from '@angular/core';
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
  selector: 'app-start-transaction-request',
  templateUrl: './start-transaction-request.component.html',
  styleUrls: ['./start-transaction-request.component.css']
})
export class StartTransactionRequestComponent implements OnInit {

  triggerReasons : any[] = [];
  IdTokenEnumTypes : any[] = [];

  @Output() StartTransactionEvent : EventEmitter<any> = new EventEmitter<any>();

  request : TransactionEventRequest = {
    eventType: TransactionEventEnumType.Started,
    meterValue: [{
        timestamp: this._wsService.formatDate(new Date()),
        sampledValue: [{
            value: 15,
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
    triggerReason: TriggerReasonEnumType.CablePluggedIn,
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
    this.request.transactionInfo.transactionId = this._wsService.guid();
    this.StartTransactionEvent.emit(this.request);
  }

}
