import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { OcppEvDriverService } from 'src/_services/ocpp-services/ocpp-ev-driver.service';
import { showOcppCommandFeedback } from 'src/_services/ocpp-services/ocpp-command-feedback';

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
      if(showOcppCommandFeedback(this._modalService, data)) this.successEvent.emit()
    },(error)=> {
      showOcppCommandFeedback(this._modalService, error);
    })
  }

}
