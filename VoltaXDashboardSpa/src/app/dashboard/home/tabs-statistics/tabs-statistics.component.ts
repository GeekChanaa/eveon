import { AfterViewInit, Component, ElementRef, OnInit, QueryList, ViewChildren } from '@angular/core';
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
export class TabsStatisticsComponent implements OnInit,AfterViewInit {
  @ViewChildren('tabsLink') tabsLink!: QueryList<ElementRef>;
  @ViewChildren('tabsItem') tabsItem!: QueryList<ElementRef>;

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

  // Charts
  energyChart: any;
  orderChart : any;
  rechargeChart : any;

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
  ) {
  }

  ngOnInit() {
    this.getEnergyData();
    this.getOrderCount();
    this.getRechargeAmount();
  }

  // Get energy data
  getEnergyData(){
    // Total Energy
    this._transactionService.getTotalEnergyConsumed().subscribe(result => this.totalEnergy = result);
    this._transactionService.getTotalEnergyConsumedToday().subscribe(result => this.totalEnergyToday = result);
    this._transactionService.getDailyEnergyConsumedLast30Days().subscribe(result =>{ 
      this.totalEnergyByDay = result;
      var categories = Object.keys(this.totalEnergyByDay);
      var values = Object.values(this.totalEnergyByDay);
      this.energyChart = this.chartOptionsConstructor(values,categories);
    });
  }

  // count order numbers
  getOrderCount(){
    this._orderService.count().subscribe(result => this.totalOrders = result);
    this._orderService.countOrdersToday().subscribe(result => this.totalOrdersToday = result);
    this._orderService.countOrdersByDay().subscribe(result => {
      this.totalOrdersByDay = result;
      var categories = Object.keys(this.totalOrdersByDay);
      var values = Object.values(this.totalOrdersByDay);
      this.orderChart = this.chartOptionsConstructor(values,categories);
    });
  }

  // count recharge amount
  getRechargeAmount(){
    this._orderService.countRechargeAmount().subscribe(result => this.totalRechargeAmount = result);
    this._orderService.countRechargeAmountToday().subscribe(result => this.totalRechargeAmountToday = result);
    this._orderService.countRechargeAmountByDay().subscribe(result => this.totalRechargeAmountByDay = result);
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
