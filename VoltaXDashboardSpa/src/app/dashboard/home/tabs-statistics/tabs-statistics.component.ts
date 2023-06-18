import { Component, OnInit } from '@angular/core';
import { OrderService } from 'src/_services/order.service';
import { TransactionService } from 'src/_services/transaction.service';
import {
  ChartComponent,
  ApexAxisChartSeries,
  ApexChart,
  ApexXAxis,
  ApexTitleSubtitle
} from "ng-apexcharts";


@Component({
  selector: 'app-tabs-statistics',
  templateUrl: './tabs-statistics.component.html',
  styleUrls: ['./tabs-statistics.component.css']
})
export class TabsStatisticsComponent implements OnInit {

  // energy consumed data:
  totalEnergy : number = 0;
  totalEnergyToday : number = 0;
  totalEnergyByDay : number[] = [];

  // Orders Count
  totalOrders : number = 0;
  totalOrdersToday : number = 0;
  totalOrdersByDay : number[] = [];

  // Recharge Amounts
  totalRechargeAmount : number = 0;
  totalRechargeAmountToday : number = 0;
  totalRechargeAmountByDay : number[] = [];

  constructor(
    private _transactionService : TransactionService,
    private _orderService : OrderService
  ) { }

  ngOnInit() {
    this.getEnergyData();
  }

  // Get energy data
  getEnergyData(){
    // Total Energy
    this._transactionService.countEnergy().subscribe(result => this.totalEnergy = result);
    this._transactionService.countEnergyToday().subscribe(result => this.totalEnergyToday = result);
    this._transactionService.countEnergyByDay().subscribe(result => this.totalEnergyByDay = result);
  }

  // count order numbers
  getOrderCount(){
    this._orderService.count().subscribe(result => this.totalOrders = result);
    this._orderService.countOrdersToday().subscribe(result => this.totalOrdersToday = result);
    this._orderService.countOrdersByDay().subscribe(result => this.totalOrdersByDay = result);
  }

  // count recharge amount
  getRechargeAmount(){
    this._orderService.countRechargeAmount().subscribe(result => this.totalRechargeAmount = result);
    this._orderService.countRechargeAmountToday().subscribe(result => this.totalRechargeAmountToday = result);
    this._orderService.countRechargeAmountByDay().subscribe(result => this.totalRechargeAmountByDay = result);
  }
}
