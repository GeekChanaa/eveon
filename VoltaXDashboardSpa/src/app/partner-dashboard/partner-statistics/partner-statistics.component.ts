import { Component, OnInit } from '@angular/core';
import { AuthService } from 'src/_services/auth.service';
import { ChargingStationService } from 'src/_services/charging-station.service';
import { OrderService } from 'src/_services/order.service';
import { TransactionService } from 'src/_services/transaction.service';

@Component({
  selector: 'app-partner-statistics',
  templateUrl: './partner-statistics.component.html',
  styleUrls: ['./partner-statistics.component.css']
})
export class PartnerStatisticsComponent implements OnInit {

  // PartnerID
  partnerID : number = 0;

  total_energy_consumed : number = 0 ;
  total_energy_consumed_last_week : number = 0 ;
  


  latestTransactions : any[] = [];

  top10ChargingStationRevenues : any[] = [];

  lastWeekEndDate : Date = new Date();
  lastWeekStartDate : Date = new Date(this.lastWeekEndDate.getFullYear(), this.lastWeekEndDate.getMonth(), this.lastWeekEndDate.getDate() - 7);

  // constructor
  constructor(
    private _transactionService : TransactionService,
    private _orderService : OrderService,
    private _chargingStationService : ChargingStationService,
    private _authService : AuthService
  ) { }

  ngOnInit() {
    this.partnerID = parseInt(this._authService.getAuthInformation().nameid);
    this.getEnergyData();
    this.getTop10ChargingStationsByRevenue();
    this.getLatestTransactions();
  }

  // get energy
  getEnergyData(){
    //total energy
    this._transactionService.partnerTotalEnergyConsumed(this.partnerID).subscribe((data) => {
      this.total_energy_consumed = data;
    })

    // percentage energy consumed this week / energy consumed last week
    this._transactionService.getPartnerTotalEnergyConsumedBetween(this.partnerID,this.lastWeekStartDate, this.lastWeekEndDate).subscribe((data) => {
      this.total_energy_consumed_last_week = data;
    })
  }

  // get Top 10 charging station revenues
  getTop10ChargingStationsByRevenue(){
    this._chargingStationService.getPartnerTop10ChargingStationsByRevenue(this.partnerID).subscribe((data) => {
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
    this._transactionService.getPartnerLatestTransactions(this.partnerID).subscribe((data) => {
      this.latestTransactions = data;
    })
  }

}
