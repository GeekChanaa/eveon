import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { OcppEvDriverService } from 'src/_services/ocpp-services/ocpp-ev-driver.service';

@Component({
  selector: 'app-request-stop-transaction',
  templateUrl: './request-stop-transaction.component.html',
  styleUrls: ['./request-stop-transaction.component.sass']
})
export class RequestStopTransactionComponent implements OnInit {

  @Input() transactionID : string = "";
  @Input() chargePointID : string = "";
  @Output() successEvent : EventEmitter<void> = new EventEmitter();

  constructor(
    private _evDriverService : OcppEvDriverService,
    private _modalService : ActionModalService
  ) { }

  ngOnInit() {
  }

  stopTransaction(){
    this._evDriverService.requestStopTransaction(this.chargePointID, {'transactionId' : this.transactionID}).subscribe((data) => {
      this._modalService.popup(ActionModalStatusEnum.Success,"Success","Transaction Stopped Successfully",4000);
      this.successEvent.emit()
    },(error)=> {
      this._modalService.popup(ActionModalStatusEnum.Error,"Something Went Wrong","Something Went wrong please try again later", 4000);
    })
  }

}
