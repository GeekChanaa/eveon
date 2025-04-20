import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TransactionListDto } from 'src/_models/_dtos/transaction-dtos/transaction-list-dto';
import { TransactionService } from 'src/_services/transaction.service';

@Component({
  selector: 'app-transactions-list',
  templateUrl: './transactions-list.component.html',
  styleUrls: ['./transactions-list.component.sass']
})
export class TransactionsListComponent implements OnInit {

  transaction: TransactionListDto = {
    connectorID: 0,
    connectorName: '',
    chargingSessionID: 0,
    startTime: new Date(),
    meterStart: 0,
    amount: 0
  };

  fields: string[] = [];
  filters : any = {
    status : ""
  };

  // Constructor
  constructor(
    private _transactionService: TransactionService,
    private _router : Router
  ) { }

  ngOnInit() {
    this._getItemFields();
  }

  getTransactionsObservable = (currentPage:  number | undefined, itemsPerPage : number | undefined, itemParams : any) => this._transactionService.getAllTransactions(currentPage, itemsPerPage, itemParams);
  deleteTransactionObservable = (id : number) => this._transactionService.deleteById(id);
  updateTransactionObservable = (id : number, model : any) => this._transactionService.edit(id, model);

  private _getItemFields() {
    if (!this.transaction || this.transaction == undefined) {
      return;
    }
    Object.keys(this.transaction ?? {}).forEach((element: string) => {
      if (typeof this.transaction?.[element] == "object" && this.transaction?.[element] != null && this.transaction?.[element].constructor.name == "Date")
        this.fields.push(element);
      if (typeof this.transaction?.[element] != "object") this.fields.push(element);
    });
  }
  

  resetFilters(){
    this.filters = {
      status : ""
    }
  }

}
