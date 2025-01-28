import { Component, HostListener, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-connector-realtime-firmware',
  templateUrl: './connector-realtime-firmware.component.html',
  styleUrls: ['./connector-realtime-firmware.component.sass']
})
export class ConnectorRealtimeFirmwareComponent implements OnInit {

  @Input() chargePoint : any = {};

  requests: string[] = [
    "PublishFirmwareRequest",
    "UpdateFirmwareRequest",
    "UnpublishFirmwareRequest",
  ];

  currentOcppAction : any = {};
  requestHandlerModalVisible : string = "";
  
  ngOnInit() {
  }


  openRequestHandlerModal(ocppAction : any){
    this.requestHandlerModalVisible = ocppAction;
  }

  closeModal(){
    this.requestHandlerModalVisible = "";
  }


  @HostListener('document:keydown.escape', ['$event'])
  handleEscapeKey(event: KeyboardEvent) {
    this.closeModal();
  }

}
