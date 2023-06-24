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

  nbr_charge_points : number = 0 ; 
  nbr_charge_points_week : number = 0 ;
  recharge_orders_total_amount : number = 0 ;
  recharge_orders_amount_percentage : number = 0 ;
  total_energy_consumed : number = 0 ;
  percentage_energy_consumed : number = 0 ;

  // constructor
  constructor(
    private _chargePointService : ChargePointService,
    private _transactionService : TransactionService,
    private _orderService : OrderService
  ) { }

  ngOnInit() {
  }

  // get energy
  getEnergyData(){
    //total energy
    this._transactionService.getTotalEnergyConsumed().subscribe((data) => {
      this.total_energy_consumed = data;
    })
    const today = new Date(); // Current date

    // Get the start date of the last week
    const lastWeekStartDate = new Date(today.getFullYear(), today.getMonth(), today.getDate() - 7);

    // get the start date of the last last week 
    const lastLastWeekStartDate = new Date(today.getFullYear(), today.getMonth(), today.getDate() - 14);

    // Get the end date of the last week (assuming today is the end of the week)
    const lastWeekEndDate = today;

    // percentage energy consumed this week / energy consumed last week
    this._transactionService.getTotalEnergyConsumedBetween(lastWeekStartDate, lastWeekEndDate).subscribe((lastWeekEnergy) => {
      this._transactionService.getTotalEnergyConsumedBetween(lastLastWeekStartDate, lastWeekStartDate).subscribe((lastLastWeekEnergy) => {
        this.percentage_energy_consumed = lastWeekEnergy / lastLastWeekEnergy;
      })
    })
  }

  // get Recharge Amount
  getRechargeAmount(){

    const today = new Date(); // Current date

    // Get the start date of the last week
    const lastWeekStartDate = new Date(today.getFullYear(), today.getMonth(), today.getDate() - 7);

    // get the start date of the last last week 
    const lastLastWeekStartDate = new Date(today.getFullYear(), today.getMonth(), today.getDate() - 14);

    // Get the end date of the last week (assuming today is the end of the week)
    const lastWeekEndDate = today;
    this._orderService.countRechargeAmountBetween(lastWeekStartDate, lastWeekEndDate).subscribe((lastWeekRechargeAmount) => {
      this._orderService.countRechargeAmountBetween(lastWeekEndDate, lastLastWeekStartDate).subscribe((lastLastWeekRechargeAmount) => {
        this.recharge_orders_amount_percentage = lastWeekRechargeAmount / lastLastWeekRechargeAmount;
      })
    })
  }

}
