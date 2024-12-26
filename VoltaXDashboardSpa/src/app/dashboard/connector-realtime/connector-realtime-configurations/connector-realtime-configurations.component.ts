import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-connector-realtime-configurations',
  templateUrl: './connector-realtime-configurations.component.html',
  styleUrls: ['./connector-realtime-configurations.component.sass']
})
export class ConnectorRealtimeConfigurationsComponent implements OnInit {

  @Input() chargePoint : any = {};

  currentOcppAction : any = {};
  requestHandlerModalVisible : string = "";

  constructor() { }

  ngOnInit() {
  }

  openRequestHanlderModal(ocppAction : any){
    this.requestHandlerModalVisible = ocppAction;
  }

  closeModal(){
    this.requestHandlerModalVisible = "";
  }

}
