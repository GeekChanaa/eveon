import { Component, OnInit } from '@angular/core';
import { ChargePointService } from 'src/_services/charge-point.service';
import { ChargingStationService } from 'src/_services/charging-station.service';
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

  rechargeOrdersChart : any = {};

  latestTransactions : any[] = [];

  top10ChargingStationRevenues : any[] = [];

  lastWeekEndDate : Date = new Date();
  lastWeekStartDate : Date = new Date(this.lastWeekEndDate.getFullYear(), this.lastWeekEndDate.getMonth(), this.lastWeekEndDate.getDate() - 7);

  // constructor
  constructor(
    private _transactionService : TransactionService,
    private _orderService : OrderService,
    private _chargingStationService : ChargingStationService
  ) { }

  ngOnInit() {
    this.getEnergyData();
    this.getRechargeAmountData();
    this.getRechargeOrdersNumbersData();
    this.getTop10ChargingStationsByRevenue();
    this.getLatestTransactions();
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

    this._orderService.countRechargeAmountByDay().subscribe((data) => {
      var categories = Object.keys(data);
      var values = Object.values(data);
      this.rechargeOrdersChart = this.chartOptionsConstructor(values,categories);
    })
  }

  // change recharge orders chart period
  getRechargeOrdersOf(period : string){
    if(period == "7days"){
      this._orderService.countbylast7Days().subscribe((data) => {
        var categories = Object.keys(data);
        var values = Object.values(data);
        this.rechargeOrdersChart = this.chartOptionsConstructor(values,categories);
      })
    }
    
    if(period == "30days"){
      this._orderService.countOrdersByDay().subscribe((data) => {
        var categories = Object.keys(data);
        var values = Object.values(data);
        this.rechargeOrdersChart = this.chartOptionsConstructor(values,categories);
      })
    }

    if(period == "12months"){
      this._orderService.countbylast12months().subscribe((data) => {
        var categories = Object.keys(data);
        var values = Object.values(data);
        this.rechargeOrdersChart = this.chartOptionsConstructor(values,categories);
      })
    }
  }

  // get Top 10 charging station revenues
  getTop10ChargingStationsByRevenue(){
    this._chargingStationService.getTop10ChargingStationsByRevenue().subscribe((data) => {
      this.top10ChargingStationRevenues = data;
    })
  }

  chartOptionsConstructor(values :any, categories : any){
    return {
      series: [
        {
          name: 'Earning',
          data: values
        }
      ],
      chart: {
        height: '200',
        type: 'line',
        toolbar: {
          show: false
        },
        fontFamily: 'Inter, sans-serif'
      },
      dataLabels: {
        enabled: false
      },
      stroke: {
        curve: 'smooth',
        width: 4
      },
      xaxis: {
        type: 'category',
        categories: categories,
        axisBorder: {
          show: false
        },
        axisTicks: {
          show: false
        },
        tooltip: {
          enabled: false
        }
      },
      grid: {
        strokeDashArray: 0,
        padding: {
          top: -20,
          right: 0,
          bottom: 0,
          left: 10
        }
      },
      legend: {
        show: false
      },
      colors: ['#2A85FF']
    };
  }


  // getting latest transactions
  getLatestTransactions(){
    this._transactionService.getLatestTransactions().subscribe((data) => {
      this.latestTransactions = data;
    })
  }

}
