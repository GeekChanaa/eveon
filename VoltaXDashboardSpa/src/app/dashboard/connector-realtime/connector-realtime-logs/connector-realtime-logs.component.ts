import { Component, Input, OnInit } from '@angular/core';
import { MessageLogService } from 'src/_services/message-log.service';
import { SignalRChargerService } from 'src/_services/signalR-charger.service';
import * as Prism from 'prismjs';
import 'prismjs/components/prism-json';
import { ActionModalService } from 'src/_services/action-modal.service';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';

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
  messageLogDisplayed? : any;
  pagination : any = {};

  eventType : string = "";

  isFiltersActive : boolean = false;

  isLoading : boolean = false;

  realtimeOn : boolean = false;

  filters : any = {
    message:""
  };

  itemParams: any = {
    orderBy : "LogTime",
    reverseOrder : "y"
  };

  searchByEventType() {
    // Update parameters in itemParams
    this.itemParams.SearchBy = ['ContentSent']; // array of fields to search in
    this.itemParams.SearchValue = '"eventType":"'+this.eventType+'"'; // the value to search for
  }

  constructor(
    private _messagLogService : MessageLogService,
    private _sRChargerService : SignalRChargerService,
    private _modalService: ActionModalService
  ) { }

  ngOnInit() {
    this.getLogs();
    this._sRChargerService.addMessageListener(this.messageReceived)
  }

  getLogs(page : number = 1){
    if(this.filters.message == "TransactionEvent" && this.eventType != "") 
      this.searchByEventType();
    this.isLoading = true;
    this.page = page;
    this._messagLogService.getChargePointMessageLogs(this.chargePoint.chargePointId,page,this.itemsPerPage, this.itemParams).subscribe((data) => {
      this.isLoading = false;
      if(data.result)
        this.messageLogs = data.result;
      if(data.pagination)
        this.pagination = data.pagination;
    },(error) => {
      this._modalService.popup(ActionModalStatusEnum.Error,"Error","Something went wrong",4000);
    })
  }

  messageReceived(msg : string){
    console.log("this is the message received");
    console.log(msg);
  }

  displayMessage(messageLog : any){
    console.log("this is the messageLog Displayed");
    console.log(messageLog)
    this.messageLogDisplayed = messageLog;
  }

  closeDiplayedMessageModal(){
    this.messageLogDisplayed = null;
  }

  realtimeOnToggle(rtOn : boolean){
    this.realtimeOn = rtOn;
  }

  applyFilters() {
    this.itemParams.FilterValue = [];
    this.itemParams.FilterBy = [];
  
    for (const key in this.filters) {
      if (this.filters[key]) {  
        this.itemParams.FilterValue.push(this.filters[key]);  
        this.itemParams.FilterBy.push(this.capitalizeFirstLetter(key));  
      }
    }
    this.getLogs(this.page);  
    this.removeActive();
  }

  resetFilters(){
    this.filters = {
      message:""
    }
  }

  removeActive(): void {
    this.isFiltersActive = false;
  }

  capitalizeFirstLetter(str: string): string {
    return str.charAt(0).toUpperCase() + str.slice(1);
  }

  toggleActive(event: Event): void {
    event.stopPropagation();
    this.isFiltersActive = !this.isFiltersActive;
  }
}
