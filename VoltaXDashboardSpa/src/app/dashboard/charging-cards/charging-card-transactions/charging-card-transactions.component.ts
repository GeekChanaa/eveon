import { Component, Input, OnInit } from '@angular/core';
import { Transaction } from 'src/_models/transaction';
import { TransactionService } from 'src/_services/transaction.service';

@Component({
  selector: 'app-charging-card-transactions',
  templateUrl: './charging-card-transactions.component.html',
  styleUrls: ['./charging-card-transactions.component.sass']
})
export class ChargingCardTransactionsComponent implements OnInit {
  @Input() cardID : number = 0;
  fields : any[] = [];

  transaction : Transaction = {
    id: 0,
    connectorID: 0,
    startTagId: '',
    startTime: new Date(),
    meterStart: 0,
    startResult: '',
    stopTagId: '',
    stopTime: new Date(),
    meterStop: 0,
    stopReason: '',
    amount: 0,
    cardID: 0,
    status: ''
  }

  constructor(
    private _transactionService : TransactionService
  ) { }

  ngOnInit() {
    this._getItemFields();
  }

  getCardTransactionsObservable = (currentPage:  number | undefined, itemsPerPage : number | undefined, itemParams : any) => this._transactionService.getCardTransactions(currentPage! , itemsPerPage! , itemParams, this.cardID);


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

}
