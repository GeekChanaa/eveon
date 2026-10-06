import { Component, Input, OnInit } from '@angular/core';
import { MessageLogService } from 'src/_services/message-log.service';
import { SignalRChargerService } from 'src/_services/signalR-charger.service';
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

  // First load shows the preloader; a refresh keeps the table and spins the button.
  isLoading : boolean = false;
  isRefreshing : boolean = false;

  realtimeOn : boolean = false;

  messageTypes : string[] = [
    'BootNotification',
    'Authorize',
    'MeterValues',
    'SecurityEventNotification',
    'StatusNotification',
    'TransactionEvent'
  ];

  quickRanges = [
    { label: '1h', hours: 1 },
    { label: '24h', hours: 24 },
    { label: '7d', hours: 24 * 7 }
  ];
  activeQuickRange : number | null = null;

  // from / to hold the <input type="datetime-local"> value: local time, "yyyy-MM-ddTHH:mm".
  filters = {
    message: "",
    eventType: "",
    from: "",
    to: ""
  };

  rangeError : string = "";

  itemParams: any = {
    orderBy : "LogTime",
    reverseOrder : "y"
  };

  constructor(
    private _messagLogService : MessageLogService,
    private _sRChargerService : SignalRChargerService,
    private _modalService: ActionModalService
  ) { }

  ngOnInit() {
    this.getLogs();
    this._sRChargerService.addMessageListener(this.messageReceived)
  }

  get hasActiveFilters() : boolean {
    return !!(this.filters.message || this.filters.eventType || this.filters.from || this.filters.to);
  }

  getLogs(page : number = 1, silent : boolean = false){
    this.page = page;
    if(silent)
      this.isRefreshing = true;
    else
      this.isLoading = true;

    this._messagLogService.getChargePointMessageLogs(this.chargePoint.chargePointId, page, this.itemsPerPage, this.buildParams()).subscribe({
      next: (data) => {
        this.isLoading = false;
        this.isRefreshing = false;
        this.messageLogs = (data.result ?? []).map(log => ({ ...log, logTime: this.asUtc(log.logTime) }));
        if(data.pagination)
          this.pagination = data.pagination;
      },
      error: () => {
        this.isLoading = false;
        this.isRefreshing = false;
        this._modalService.popup(ActionModalStatusEnum.Error,"Error","Something went wrong",4000);
      }
    })
  }

  refresh(){
    if(this.isLoading || this.isRefreshing)
      return;

    // A quick range is relative to "now", so move its window forward before reloading.
    if(this.activeQuickRange != null)
      this.setRangeFields(this.activeQuickRange);

    this.getLogs(this.page, true);
  }

  applyFilters(){
    this.rangeError = "";
    if(this.filters.from && this.filters.to && new Date(this.filters.from) > new Date(this.filters.to)){
      this.rangeError = "“From” must be before “To”";
      return;
    }
    if(this.filters.message != 'TransactionEvent')
      this.filters.eventType = "";

    this.getLogs(1);
  }

  onRangeEdited(){
    this.activeQuickRange = null;
    this.applyFilters();
  }

  setQuickRange(hours : number){
    this.activeQuickRange = hours;
    this.setRangeFields(hours);
    this.applyFilters();
  }

  resetFilters(){
    this.filters = { message: "", eventType: "", from: "", to: "" };
    this.activeQuickRange = null;
    this.applyFilters();
  }

  messageReceived(msg : string){
    console.log("this is the message received");
    console.log(msg);
  }

  displayMessage(messageLog : any){
    this.messageLogDisplayed = messageLog;
  }

  closeDiplayedMessageModal(){
    this.messageLogDisplayed = null;
  }

  realtimeOnToggle(rtOn : boolean){
    this.realtimeOn = rtOn;
  }

  private buildParams() : any {
    const params : any = {
      orderBy: this.itemParams.orderBy,
      reverseOrder: this.itemParams.reverseOrder,
      FilterBy: [],
      FilterValue: []
    };

    if(this.filters.message){
      params.FilterBy.push("Message");
      params.FilterValue.push(this.filters.message);
    }

    if(this.filters.message == "TransactionEvent" && this.filters.eventType){
      params.SearchBy = ['ContentSent'];
      params.SearchValue = '"eventType":"' + this.filters.eventType + '"';
    }

    // The inputs are local time; the API compares against UTC. They only go down to the
    // minute, so "To" covers the whole minute it names.
    if(this.filters.from)
      params.from = new Date(this.filters.from).toISOString();
    if(this.filters.to)
      params.to = new Date(new Date(this.filters.to).getTime() + 59999).toISOString();

    return params;
  }

  private setRangeFields(hours : number){
    const now = new Date();
    this.filters.to = this.toInputValue(now);
    this.filters.from = this.toInputValue(new Date(now.getTime() - hours * 3600 * 1000));
  }

  private toInputValue(date : Date) : string {
    const pad = (n : number) => n.toString().padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }

  // LogTime is stored in UTC but comes back without a zone marker, which the date pipe
  // would read as local time and so disagree with the From / To filter.
  private asUtc(value : string) : string {
    if(!value || /Z$|[+-]\d{2}:?\d{2}$/.test(value))
      return value;
    return value + 'Z';
  }
}
