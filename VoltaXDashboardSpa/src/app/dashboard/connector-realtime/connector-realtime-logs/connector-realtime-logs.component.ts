import { Component, Input, OnInit } from '@angular/core';
import { MessageLogService } from 'src/_services/message-log.service';
import { SignalRChargerService } from 'src/_services/signalR-charger.service';

@Component({
  selector: 'app-connector-realtime-logs',
  templateUrl: './connector-realtime-logs.component.html',
  styleUrls: ['./connector-realtime-logs.component.sass']
})
export class ConnectorRealtimeLogsComponent implements OnInit {

  @Input() chargePoint : any = {};
  page : number = 1;
  itemsPerPage : number = 20;
  messageLogs : any[] = [];

  itemParams: any = {
    orderBy : "LogTime",
    reverseOrder : "y"
  };

  search(val: string) {
    // Update parameters in itemParams
    this.itemParams.SearchBy = ['Name', 'Address']; // array of fields to search in
    this.itemParams.SearchValue = 'Gut'; // the value to search for
    this.getLogs();
  }

  constructor(
    private _messagLogService : MessageLogService,
    private _sRChargerService : SignalRChargerService
  ) { }

  ngOnInit() {
    this.getLogs();
    this._sRChargerService.addMessageListener(this.messageReceived)
  }

  getLogs(){
    this._messagLogService.getChargePointMessageLogs(this.chargePoint.chargePointId,this.page,this.itemsPerPage, this.itemParams).subscribe((data) => {
      if(data.result)
        this.messageLogs = data.result;
    })
  }

  messageReceived(msg : string){
    console.log("this is the message received");
    console.log(msg);
  }

}
