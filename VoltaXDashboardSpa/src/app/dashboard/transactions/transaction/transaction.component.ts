import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { TransactionService } from 'src/_services/transaction.service';

@Component({
  selector: 'app-transaction',
  templateUrl: './transaction.component.html',
  styleUrls: ['./transaction.component.sass']
})
export class TransactionComponent implements OnInit {
  
    transactionID : string = "";
    transactionLoaded : boolean = false;
    transaction: any = {};
  
  
  
    constructor(
      private _route: ActivatedRoute,
      private _transactionService : TransactionService
    ) {
     }
  
    ngOnInit() {
      var idParam = this._route.snapshot.paramMap.get('id')
      if (idParam != null) {
        var id = idParam;
        this.getChargePointByID(id);
      }
    }
  
  
    getChargePointByID(uid : string){
      this.transactionID = uid;
      this._transactionService.getTransactionByID(uid).subscribe((cs) => {
        this.transaction = cs;
        this.transactionLoaded = true;
      })
    }
}
