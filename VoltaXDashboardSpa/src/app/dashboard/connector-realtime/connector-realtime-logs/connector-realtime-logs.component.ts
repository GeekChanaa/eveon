import { Component, Input, OnInit } from '@angular/core';
import { MessageLogService } from 'src/_services/message-log.service';

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

  constructor(
    private _messagLogService : MessageLogService
  ) { }

  ngOnInit() {
    this.getLogs();
  }

  getLogs(){
    this._messagLogService.getChargePointMessageLogs(this.chargePoint.chargePointId,this.page,this.itemsPerPage).subscribe((data) => {
      if(data.result)
        this.messageLogs = data.result;

      console.log(data.result);
    })
  }

}
