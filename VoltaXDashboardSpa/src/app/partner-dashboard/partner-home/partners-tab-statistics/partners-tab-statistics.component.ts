import { Component, ElementRef, OnInit, QueryList, ViewChildren } from '@angular/core';
import { AuthService } from 'src/_services/auth.service';
import { PartnerStatisticsService } from 'src/_services/partner-statistics.service';
import { TransactionService } from 'src/_services/transaction.service';

@Component({
  selector: 'app-partners-tab-statistics',
  templateUrl: './partners-tab-statistics.component.html',
  styleUrls: ['./partners-tab-statistics.component.css']
})
export class PartnersTabStatisticsComponent implements OnInit {

  
  @ViewChildren('tabsLink') tabsLink!: QueryList<ElementRef>;
  @ViewChildren('tabsItem') tabsItem!: QueryList<ElementRef>;

  // Chart data
  energyData: any[] = [];
  energyCategories: any[] = [];
  totalRevenueData: any[] = [];
  totalRevenueCategories: any[] = [];

  // Energy consumed data:
  totalEnergy: number = 0;
  totalEnergyToday: number = 0;
  totalEnergyByDay: number[] = [];


  // Revenue:
  totalRevenue: number = 0;
  totalRevenueToday: number = 0;
  totalRevenueByDay: { [date: string]: number } = {};
  totalRevenueByMonth: { [monthYear: string]: number } = {};
  revenueChart: any;
  revenueMonthlyChart: any;

  // Charged Minutes:
  totalChargedMinutes: number = 0;
  totalChargedMinutesToday: number = 0;
  totalChargedMinutesByDay: number[] = [];

  // Charging Sessions:
  totalChargingSessions: number = 0;
  totalChargingSessionsToday: number = 0;
  totalChargingSessionsByDay: number[] = [];

  // Charts:
  energyChart: any;
  chargedMinutesChart: any;
  chargingSessionsChart: any;

  partnerID : number = 0;

  constructor(
    private _authService : AuthService,
    private _transactionService: TransactionService,
    private _statisticsService:  PartnerStatisticsService
  ) { }

  ngOnInit() {
    var user = this._authService.decodedToken;
    this.partnerID = parseInt(user.partnerID);
    this.getPartnerStatistics();
  }

  ngAfterViewInit() {
    this.changeTabs();
  }

  // Get Partner Statistics
  getPartnerStatistics() {
    this.getRevenueData();
    this.getEnergyData();
    this.getChargedMinutesData();
    this.getChargingSessionsData();
  }

  // Revenue data
  getRevenueData() {
    // Total Revenue
    this._statisticsService.getTotalRevenue(this.partnerID).subscribe((result : any) => this.totalRevenue = result);
    this._statisticsService.getTotalRevenueToday(this.partnerID).subscribe((result : any) => this.totalRevenueToday = result);
    this._statisticsService.getDailyRevenueLast30Days(this.partnerID).subscribe((result : any) => {
      this.totalRevenueByDay = result;
      const categories = Object.keys(this.totalRevenueByDay).map(date => {
        const d = new Date(date);
        return `${d.getDate()} ${d.toLocaleString('default', { month: 'short' })} ${d.getFullYear()}`;
      });
      const values = Object.values(this.totalRevenueByDay).map(value => parseFloat(value.toFixed(2)));
      this.revenueChart = this.chartOptionsConstructor(values, categories);
    });
    this._statisticsService.getMonthlyRevenueLastYear(this.partnerID).subscribe((result : any) => {
      this.totalRevenueByMonth = result;
      const categories = Object.keys(this.totalRevenueByMonth);
      const values = Object.values(this.totalRevenueByMonth).map(value => parseFloat(value.toFixed(2)));
      this.revenueMonthlyChart = this.chartOptionsConstructor(values, categories);
    });
  }

  // Energy consumed data
  getEnergyData() {
    this._statisticsService.getTotalEnergyConsumed(this.partnerID).subscribe((result : any) => this.totalEnergy = result);
    this._statisticsService.getTotalEnergyConsumedToday(this.partnerID).subscribe((result : any) => this.totalEnergyToday = result);
    this._statisticsService.getDailyEnergyConsumedLast30Days(this.partnerID).subscribe((result : any) => {
      this.totalEnergyByDay = result;
      const categories = Object.keys(this.totalEnergyByDay).map(date => {
        const d = new Date(date);
        return `${d.getDate()} ${d.toLocaleString('default', { month: 'short' })} ${d.getFullYear()}`;
      });
      const values = Object.values(this.totalEnergyByDay).map(value => parseFloat(value.toFixed(2)));
      this.energyChart = this.chartOptionsConstructor(values, categories);
    });
  }

  // Charged Minutes data
  getChargedMinutesData() {
    this._statisticsService.getTotalChargedMinutes(this.partnerID).subscribe((result : any) => this.totalChargedMinutes = result);
    this._statisticsService.getTotalChargedMinutesToday(this.partnerID).subscribe((result : any) => this.totalChargedMinutesToday = result);
    this._statisticsService.getDailyChargedMinutesLast30Days(this.partnerID).subscribe((result : any) => {
      this.totalChargedMinutesByDay = result;
      const categories = Object.keys(this.totalChargedMinutesByDay).map(date => {
        const d = new Date(date);
        return `${d.getDate()} ${d.toLocaleString('default', { month: 'short' })} ${d.getFullYear()}`;
      });
      const values = Object.values(this.totalChargedMinutesByDay).map(value => parseFloat(value.toFixed(2)));
      this.chargedMinutesChart = this.chartOptionsConstructor(values, categories);
    });
  }

  // Charging Sessions data
  getChargingSessionsData() {
    this._statisticsService.getTotalChargingSessions(this.partnerID).subscribe((result : any) => this.totalChargingSessions = result);
    this._statisticsService.getTotalChargingSessionsToday(this.partnerID).subscribe((result : any) => this.totalChargingSessionsToday = result);
    this._statisticsService.getDailyChargingSessionsLast30Days(this.partnerID).subscribe((result : any) => {
      this.totalChargingSessionsByDay = result;
      const categories = Object.keys(this.totalChargingSessionsByDay).map(date => {
        const d = new Date(date);
        return `${d.getDate()} ${d.toLocaleString('default', { month: 'short' })} ${d.getFullYear()}`;
      });
      const values = Object.values(this.totalChargingSessionsByDay).map(value => parseFloat(value.toFixed(2)));
      this.chargingSessionsChart = this.chartOptionsConstructor(values, categories);
    });
  }

  // Chart Configuration Constructor
  chartOptionsConstructor(values: any, categories: any) {
    return {
      series: [
        {
          name: 'Earnings',
          data: values
        }
      ],
      chart: {
        height: '200',
        type: 'line',
        toolbar: { show: false },
        fontFamily: 'Inter, sans-serif'
      },
      dataLabels: { enabled: false },
      stroke: {
        curve: 'smooth',
        width: 4
      },
      xaxis: {
        type: 'category',
        categories: categories,
        axisBorder: { show: false },
        axisTicks: { show: false },
        tooltip: { enabled: false }
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
      legend: { show: false },
      colors: ['#2A85FF']
    };
  }

  // Tab change function
  changeTabs() {
    const tabsLinks = this.tabsLink.toArray();
    const tabsItems = this.tabsItem.toArray();
    
    tabsLinks.forEach((tabLink : any, index : any) => {
      tabLink.nativeElement.addEventListener('click', (e: any) => {
        e.preventDefault();

        // Reset all tabs and content
        tabsLinks.forEach((tab : any) => tab.nativeElement.classList.remove('active'));
        tabsItems.forEach((item : any) => item.nativeElement.style.display = 'none');

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

}
