import { Component, Input, OnInit } from '@angular/core';
import { Action } from 'rxjs/internal/scheduler/Action';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { Pagination } from 'src/_models/pagination';
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

  paginationPages: any[] = [];
  
  
  pagination: Pagination = {
    currentPage: 0,
    itemsPerPage: 0,
    totalItems: 0,
    totalPages: 0
  }

  itemParams : any = {};

  itemsPerPage: number = 10;
  currentPage: number = 1;

  constructor(
    private _transactionService: TransactionService,
    private _modalService : ActionModalService
  ) { }

  ngOnInit() {
    this.getTransactions();
  }

  // Get ChargePoint Transactions
  getTransactions(page : number = 1){
    this._transactionService.getChargePointTransactions(this.chargePoint.id,page, this.itemsPerPage, this.itemParams).subscribe((data) => {
      if(data.result)
        this.transactions = data.result;
      if(data.pagination)
        this.pagination = data.pagination;
    },(error) => {
    })
  }

  closeModal(){
    this.stoppingTransactionID = undefined;
  }

}
