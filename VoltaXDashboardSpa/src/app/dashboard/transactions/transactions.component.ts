import { Component, OnInit } from '@angular/core';
import { Transaction } from 'src/_models/transaction';
import { OrderService } from 'src/_services/order.service';
import { TransactionService } from 'src/_services/transaction.service';

@Component({
  selector: 'app-transactions',
  templateUrl: './transactions.component.html',
  styleUrls: ['./transactions.component.sass']
})
export class TransactionsComponent implements OnInit {
  ngOnInit(): void {
    throw new Error('Method not implemented.');
  }


}
