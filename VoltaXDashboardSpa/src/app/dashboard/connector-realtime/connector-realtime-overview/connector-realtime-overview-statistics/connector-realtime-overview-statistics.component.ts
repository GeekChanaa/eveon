import { Component, ElementRef, Input, OnInit, QueryList, ViewChildren } from '@angular/core';
import { OrderService } from 'src/_services/order.service';
import { TransactionService } from 'src/_services/transaction.service';
import {
  ChartComponent,
  ApexAxisChartSeries,
  ApexChart,
  ApexXAxis,
  ApexTitleSubtitle
} from "ng-apexcharts";
import { ChargingSessionService } from 'src/_services/charging-session.service';

@Component({
  selector: 'app-connector-realtime-overview-statistics',
  templateUrl: './connector-realtime-overview-statistics.component.html',
  styleUrls: ['./connector-realtime-overview-statistics.component.css']
})
export class ConnectorRealtimeOverviewStatisticsComponent implements OnInit {
  @ViewChildren('tabsLink') tabsLink!: QueryList<ElementRef>;
  @ViewChildren('tabsItem') tabsItem!: QueryList<ElementRef>;

  @Input() chargePointID : number = 0

  // Chart data
  energyData : any[] = [];
  energyCategories : any[] = [];
  ordersData : any[] = [];
  ordersCategories : any[] = [];
  rechargeAmountData : any[] = [];
  rechargeAmountCategories : any[] = [];


  ngAfterViewInit() {
    this.changeTabs();
  }

  energyChart: any;
  chargingSessionsChart : any;
  

  // energy consumed data:
  totalEnergy : number = 0;
  totalEnergyToday : number = 0;
  totalEnergyByDay : number[] = [];

  nbrChargingSessions : number = 0;
  nbrChargingSessionsToday : number = 0;
  nbrChargingSessionsByDay : number[] = [];

  constructor(
    private _transactionService : TransactionService,
    private _chargingSessionService:  ChargingSessionService,
    private _orderService : OrderService
  ) {
  }

  ngOnInit() {
    this.getEnergyData();
    this.getChargingSessionsData();
  }

  getEnergyData(){
    this._transactionService.getTotalEnergyConsumed().subscribe(result => this.totalEnergy = result);
    this._transactionService.getTotalEnergyConsumedToday().subscribe(result => this.totalEnergyToday = result);
    this._transactionService.getDailyEnergyConsumedLast30Days().subscribe(result =>{ 
      this.totalEnergyByDay = result;
      var categories = Object.keys(this.totalEnergyByDay);
      categories = categories.map(date => {
        const d = new Date(date);
        return `${d.getDate()} ${d.toLocaleString('default', { month: 'short' })} ${d.getFullYear()}`;
      });
      var values = Object.values(this.totalEnergyByDay);
      values = values.map(value => parseFloat(value.toFixed(2)));
      this.energyChart = this.chartOptionsConstructor(values,categories);
    });
  }

  getChargingSessionsData(){
    this._chargingSessionService.getChargePointNbrChargingSessions(this.chargePointID).subscribe(result => this.nbrChargingSessions = result);
    this._chargingSessionService.getChargePointNbrChargingSessionsToday(this.chargePointID).subscribe(result => this.nbrChargingSessionsToday = result);
    this._chargingSessionService.getChargePointNbrChargingSessionsLast30Days(this.chargePointID).subscribe(result =>{ 
      this.nbrChargingSessionsByDay = result;
      var categories = Object.keys(this.nbrChargingSessionsByDay);
      categories = categories.map(date => {
        const d = new Date(date);
        return `${d.getDate()} ${d.toLocaleString('default', { month: 'short' })} ${d.getFullYear()}`;
      });
      var values = Object.values(this.nbrChargingSessions);
      values = values.map(value => parseFloat(value.toFixed(2)));
      this.chargingSessionsChart = this.chartOptionsConstructor(values,categories);
    });
  }

  changeTabs() {
    const tabsLinks = this.tabsLink.toArray();
    const tabsItems = this.tabsItem.toArray();

    tabsLinks.forEach((tabLink, index) => {
      tabLink.nativeElement.addEventListener('click', (e : any) => {
        e.preventDefault();

        // Reset all tabs and content
        tabsLinks.forEach((tab) => tab.nativeElement.classList.remove('active'));
        tabsItems.forEach((item) => item.nativeElement.style.display = 'none');

        // Activate clicked tab and its content
        tabLink.nativeElement.classList.add('active');
        tabsItems[index].nativeElement.style.display = 'block';
      });
    });

    // Activate the first tab
    if (tabsLinks[0]) {
      tabsLinks[0].nativeElement.click();
    }
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

}
