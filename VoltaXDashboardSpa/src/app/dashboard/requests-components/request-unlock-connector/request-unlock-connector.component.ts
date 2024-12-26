import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { OcppEvDriverService } from 'src/_services/ocpp-services/ocpp-ev-driver.service';

@Component({
  selector: 'app-request-unlock-connector',
  templateUrl: './request-unlock-connector.component.html',
  styleUrls: ['./request-unlock-connector.component.sass']
})
export class RequestUnlockConnectorComponent implements OnInit {

  @Input() chargePointID : string = "";
  @Output() successEvent : EventEmitter<void> = new EventEmitter();

  cards : any[] = [];
  tokenID : number = 0;
  remoteStartID : number = 0;
  constructor(
    private _evDriverService : OcppEvDriverService,
    private _modalService: ActionModalService
  ) { }

  ngOnInit() {
  }

  startTransaction(){
    let unlockConnectorRequest = {
      remoteStartID : this.remoteStartID ,
      idToken : {
        idToken : this.tokenID
      }
    };
    this._evDriverService.unlockConnector(this.chargePointID, unlockConnectorRequest).subscribe((data) => {
      this._modalService.popup(ActionModalStatusEnum.Success,"Success","Transaction Started Successfully",4000);
      this.successEvent.emit()
    },(error)=> {
      this._modalService.popup(ActionModalStatusEnum.Error,"Something Went Wrong","Something Went wrong please try again later", 4000);
    })
  }

}
