import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { OcppTransactionService } from 'src/_services/ocpp-services/ocpp-transaction.service';

@Component({
  selector: 'app-get-transaction-status-request',
  templateUrl: './get-transaction-status-request.component.html',
  styleUrls: ['./get-transaction-status-request.component.sass']
})
export class GetTransactionStatusRequestComponent implements OnInit {

  @Input() chargePointID! : string;
  @Output() successEvent : EventEmitter<void> = new EventEmitter();
  @Input() cpID! : number;
  request : any = {
    id : [],
    log : {}
  } ;
  isLoading : boolean = false;

  monitorID! : number;

  constructor(
    private _ocppTransactionService: OcppTransactionService,
    private _modalService: ActionModalService
  ) { }

  ngOnInit() {
  }

  getTransactionStatusRequest(){
    this.isLoading = true;
    this._ocppTransactionService.getTransactionStatus(this.chargePointID, this.request).subscribe((data) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Success,"Success !", "Request Sent successfully ! ",4000);
      this.successEvent.emit();
    },(error) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Error, "Error !","Something went wrong please contact your system administrator",4000);
    })
  }
}
