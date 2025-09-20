import { Component, OnInit } from '@angular/core';
import { ChargingStationService } from 'src/_services/charging-station.service';
import { OrderService } from 'src/_services/order.service';
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

  // Statistics data
  recharge_orders_total_amount: number = 0;
  recharge_orders_amount_last_week: number = 0;
  total_energy_consumed: number = 0;
  total_energy_consumed_last_week: number = 0;
  total_charging_sessions: number = 0;
  total_charging_sessions_today: number = 0;

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

  // Mock data for sections not covered by service
  top10ChargingStationRevenues: any[] = [
    { chargingStationID: 'CS-001', revenue: 1250.50 },
    { chargingStationID: 'CS-002', revenue: 980.25 },
    { chargingStationID: 'CS-003', revenue: 875.75 }
  ];

  latestTransactions: any[] = [
    {
      startTime: new Date(),
      stopTime: new Date(),
      meterStart: 100,
      meterStop: 125,
      amount: 45.50
    }
  ];

  constructor(
    private _partnerStatisticsService: PartnerStatisticsService
  ) { }

  ngOnInit(): void {
    this.loadStatistics();
    this.getRechargeOrdersOf('7days'); // Load default chart data
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
