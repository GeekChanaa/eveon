import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Order } from 'src/_models/order';
import { InvoiceService } from 'src/_services/invoice.service';
import { OrderService } from 'src/_services/order.service';

@Component({
  selector: 'app-recharge-orders',
  templateUrl: './recharge-orders.component.html',
  styleUrls: ['./recharge-orders.component.sass']
})
export class RechargeOrdersComponent implements OnInit {
  ngOnInit(): void {
    throw new Error('Method not implemented.');
  }

  

}
