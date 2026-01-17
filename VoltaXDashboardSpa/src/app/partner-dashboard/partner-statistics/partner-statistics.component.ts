import { Component, OnInit } from '@angular/core';
import { Pagination } from 'src/_models/pagination';
import { ChargingSessionService } from 'src/_services/charging-session.service';
import { ChargingStationService } from 'src/_services/charging-station.service';
import { OrderService } from 'src/_services/order.service';
import { PartnerChargingStationService } from 'src/_services/partner-services/partner-charging-stations.service';
import { PartnerStatisticsService } from 'src/_services/partner-statistics.service';
import { TransactionService } from 'src/_services/transaction.service';

@Component({
  selector: 'app-partner-statistics',
  templateUrl: './partner-statistics.component.html',
  styleUrls: ['./partner-statistics.component.sass']
})
export class PartnerStatisticsComponent implements OnInit {

  // Partner ID - you should get this from your authentication/routing service
  partnerID: number = 1; // Replace with actual partner ID
  itemsPerPage: number = 20;
  page: number = 1;


  // Statistics data
  recharge_orders_total_amount: number = 0;
  recharge_orders_amount_last_week: number = 0;
  total_energy_consumed: number = 0;
  total_energy_consumed_last_week: number = 0;
  total_charging_sessions: number = 0;
  total_charging_sessions_today: number = 0;

  itemParams: any = {}

  // Chart configuration
  rechargeOrdersChart: any = {
    series: [{
      name: 'Charging Sessions',
      data: []
    }],
    chart: {
      type: 'area',
      height: 200,
      toolbar: {
        show: false
      }
    },
    xaxis: {
      categories: []
    },
    stroke: {
      curve: 'smooth',
      width: 2
    },
    dataLabels: {
      enabled: false
    },
    colors: ['#8B5CF6'],
    legend: {
      show: false
    },
    grid: {
      show: true,
      borderColor: '#f1f5f9',
      strokeDashArray: 3
    }
  };

  pagination: Pagination = {
    currentPage: 0,
    itemsPerPage: 0,
    totalItems: 0,
    totalPages: 0
  }

  // Mock data for sections not covered by service
  top10ChargingStationRevenues: any[] = [];

  latestChargingSessions: any[] = [];

  constructor(
    private _partnerStatisticsService: PartnerStatisticsService,
    private _partnerChargingStationService: PartnerChargingStationService,
    private _chargingSessionService: ChargingSessionService
  ) { }

  ngOnInit(): void {
    this.loadStatistics();
    this.getRechargeOrdersOf('7days');
    this.getTop10ChargingStationsByRevenue();
    this.itemParams = {
      orderBy: "StartDate"
    }
    this.getPartnerChargingSessions();
  }

  getTop10ChargingStationsByRevenue() {
    this._partnerChargingStationService.getTop10ChargingStations(this.partnerID).subscribe((data) => {
      console.log("this is the top 10 charging Stations");
      console.log(data.result);
      if (data.result != null) {
        this.top10ChargingStationRevenues = data.result
      }

    });
  }

  getPartnerChargingSessions(page: number = 1) {
    this._chargingSessionService.getAllPartnerChargingSessions(this.partnerID, page, this.itemsPerPage, this.itemParams).subscribe((data) => {
      console.log("this is the data");
      console.log(data.result);
      if (data.result)
        this.latestChargingSessions = data.result;
      if (data.pagination)
        this.pagination = data.pagination;
    })
  }

  loadStatistics(): void {
    // Load total revenue
    this._partnerStatisticsService.getTotalRevenue(this.partnerID).subscribe({
      next: (data: any) => {
        this.recharge_orders_total_amount = data;
      },
      error: (error: any) => console.error('Error loading total revenue:', error)
    });

    // Load today's revenue for comparison
    this._partnerStatisticsService.getTotalRevenueToday(this.partnerID).subscribe({
      next: (data: any) => {
        this.recharge_orders_amount_last_week = data; // Using today's data as week indicator
      },
      error: (error: any) => console.error('Error loading today revenue:', error)
    });

    // Load total energy consumed
    this._partnerStatisticsService.getTotalEnergyConsumed(this.partnerID).subscribe({
      next: (data: any) => {
        this.total_energy_consumed = data;
      },
      error: (error: any) => console.error('Error loading total energy:', error)
    });

    // Load today's energy consumed
    this._partnerStatisticsService.getTotalEnergyConsumedToday(this.partnerID).subscribe({
      next: (data: any) => {
        this.total_energy_consumed_last_week = data;
      },
      error: (error: any) => console.error('Error loading today energy:', error)
    });

    // Load total charging sessions
    this._partnerStatisticsService.getTotalChargingSessions(this.partnerID).subscribe({
      next: (data: any) => {
        this.total_charging_sessions = data;
      },
      error: (error: any) => console.error('Error loading total sessions:', error)
    });

    // Load today's charging sessions
    this._partnerStatisticsService.getTotalChargingSessionsToday(this.partnerID).subscribe({
      next: (data: any) => {
        this.total_charging_sessions_today = data;
      },
      error: (error: any) => console.error('Error loading today sessions:', error)
    });
  }

  getRechargeOrdersOf(period: string): void {
    switch (period) {
      case '7days':
        this.loadDailyChargingSessions();
        break;
      case '30days':
        this.loadDailyChargingSessions();
        break;
      case '12months':
        this.loadMonthlyChargingSessions();
        break;
    }
  }

  private loadDailyChargingSessions(): void {
    this._partnerStatisticsService.getDailyChargingSessionsLast30Days(this.partnerID).subscribe({
      next: (data: any) => {
        const dates = Object.keys(data);
        const values = Object.values(data);

        this.rechargeOrdersChart = {
          ...this.rechargeOrdersChart,
          series: [{
            name: 'Charging Sessions',
            data: values
          }],
          xaxis: {
            categories: dates.map(date => {
              const d = new Date(date);
              return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
            })
          }
        };
      },
      error: (error: any) => console.error('Error loading daily sessions:', error)
    });
  }

  private loadMonthlyChargingSessions(): void {
    // Since there's no monthly charging sessions endpoint, we'll use energy data
    this._partnerStatisticsService.getMonthlyEnergyConsumedLastYear(this.partnerID).subscribe({
      next: (data: any) => {
        const months = Object.keys(data);
        const values = Object.values(data);

        this.rechargeOrdersChart = {
          ...this.rechargeOrdersChart,
          series: [{
            name: 'Energy Consumed (kWh)',
            data: values
          }],
          xaxis: {
            categories: months
          }
        };
      },
      error: (error: any) => console.error('Error loading monthly data:', error)
    });
  }

}
