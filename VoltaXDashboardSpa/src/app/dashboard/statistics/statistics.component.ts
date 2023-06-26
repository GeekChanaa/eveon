import { Component, OnInit } from '@angular/core';
import { ChargePointService } from 'src/_services/charge-point.service';
import { OrderService } from 'src/_services/order.service';
import { TransactionService } from 'src/_services/transaction.service';

@Component({
  selector: 'app-statistics',
  templateUrl: './statistics.component.html',
  styleUrls: ['./statistics.component.css']
})
export class StatisticsComponent implements OnInit {

  nbr_recharge_orders : number = 0 ; 
  nbr_recharge_orders_last_week : number = 0 ;
  recharge_orders_total_amount : number = 0 ;
  recharge_orders_amount_last_week : number = 0 ;
  total_energy_consumed : number = 0 ;
  total_energy_consumed_last_week : number = 0 ;

  lastWeekEndDate : Date = new Date();
  lastWeekStartDate : Date = new Date(this.lastWeekEndDate.getFullYear(), this.lastWeekEndDate.getMonth(), this.lastWeekEndDate.getDate() - 7);

  // constructor
  constructor(
    private _transactionService : TransactionService,
    private _orderService : OrderService
  ) { }

  ngOnInit() {
    this.getEnergyData();
    this.getRechargeAmountData();
    this.getRechargeOrdersNumbersData();
  }

  // get energy
  getEnergyData(){
    //total energy
    this._transactionService.getTotalEnergyConsumed().subscribe((data) => {
      this.total_energy_consumed = data;
    })

    // percentage energy consumed this week / energy consumed last week
    this._transactionService.getTotalEnergyConsumedBetween(this.lastWeekStartDate, this.lastWeekEndDate).subscribe((data) => {
      this.total_energy_consumed_last_week = data;
    })
  }



  // Get number of charge points data
  getRechargeAmountData(){

    // total recharge amount of all time
    this._orderService.countRechargeAmount().subscribe((data) => {
      this.recharge_orders_total_amount = data;
    })
    
    // recharge amount  by eur lat week
    this._orderService.countRechargeAmountBetween(this.lastWeekStartDate, this.lastWeekEndDate).subscribe((data) => {
      this.recharge_orders_amount_last_week = data
    })
  }

  // get recharge orders umber
  getRechargeOrdersNumbersData(){
    this._orderService.count().subscribe((data) => {
      this.nbr_recharge_orders = data
    });
  }

}
