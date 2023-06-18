import { Component, OnInit } from '@angular/core';
import { Transaction } from 'src/_models/transaction';
import { OrderService } from 'src/_services/order.service';
import { TransactionService } from 'src/_services/transaction.service';

@Component({
  selector: 'app-transactions',
  templateUrl: './transactions.component.html',
  styleUrls: ['./transactions.component.css']
})
export class TransactionsComponent implements OnInit {

  // Data
  data : any[] = [];

  // Fields
  fields : string[] = [];

  // Page params
  itemsPerPage : number = 20;
  currentPage : number = 1;

  transaction : Transaction = {
    id: 0,
    userID: 0,
    chargePointID: 0,
    startTime: new Date(),
    endTime: new Date(),
    energyConsumed: 0,
    paymentAmount: 0,
    transactionStatus: '',
    user: null,
    chargePoint: null
  }

  // Constructor
  constructor(
    private _transactionService : TransactionService
  ) { }

  ngOnInit() {
    this._getItemFields();
    this.getAll();
  }

  
  // Getting All Products
  getAll(){
    this._transactionService.getAll(this.currentPage,this.itemsPerPage).subscribe(data => {
      if (data.result) {
        this.data = data.result;
      }
    })
  }

  // Getting Item Fields
  private _getItemFields(){
    // Ensure this.chargingStation is defined
    if (!this.transaction || this.transaction == undefined) {
      return;
    }
    // Getting item fields
    Object.keys(this.transaction ?? {}).forEach((element : string) => {
      console.log(element); 
      if(typeof this.transaction?.[element] == "object" && this.transaction?.[element] != null && this.transaction?.[element].constructor.name == "Date")
      this.fields.push(element);
      if(typeof this.transaction?.[element] != "object") this.fields.push(element);
    });
  }

  // Deleting the item

  // Next page
  nextPage(){
    this.currentPage++;
    this.getAll();
  }

  // Previous Page
  previousPage(){
    this.currentPage--;
    this.getAll();
  }

}
