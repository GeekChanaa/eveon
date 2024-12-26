import { Component, Input, OnInit } from '@angular/core';
import { Action } from 'rxjs/internal/scheduler/Action';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { TransactionService } from 'src/_services/transaction.service';

@Component({
  selector: 'app-connector-realtime-transactions',
  templateUrl: './connector-realtime-transactions.component.html',
  styleUrls: ['./connector-realtime-transactions.component.sass']
})
export class ConnectorRealtimeTransactionsComponent implements OnInit {

  @Input() chargePoint : any = {};

  transactions : any[] = [];
  isHovered : boolean = false;
  stoppingTransactionID? : string;

  constructor(
    private _transactionService: TransactionService,
    private _modalService : ActionModalService
  ) { }

  ngOnInit() {
    this.getTransactions();
  }

  // Get ChargePoint Transactions
  getTransactions(){
    this._transactionService.getChargePointTransactions(this.chargePoint.chargePointId).subscribe((data) => {
      this.transactions = data;
    },(error) => {
    })
  }

  closeModal(){
    this.stoppingTransactionID = undefined;
  }

}
