import { Component, Input, OnInit } from '@angular/core';
import { ChargePointStatisticsSummaryDto } from 'src/_models/_dtos/statistics-dtos/charge-point-summary-statistics-dto';
import { StatisticsService } from 'src/_services/statistics.service';

@Component({
  selector: 'app-connector-realtime-overview',
  templateUrl: './connector-realtime-overview.component.html',
  styleUrls: ['./connector-realtime-overview.component.sass']
})
export class ConnectorRealtimeOverviewComponent implements OnInit {

  @Input() ChargePoint : any = {}

  statisticsSummary? : ChargePointStatisticsSummaryDto;
  summaryIsLoading : boolean = false;

  constructor(
    private _statisticsService : StatisticsService
  ) { }

  ngOnInit() {
    this.getStatistics();
  }

  getStatistics(){
    this.summaryIsLoading = true;
    this._statisticsService.getChargePointStatisticsSummary(this.ChargePoint.id).subscribe((data) => {
      this.summaryIsLoading = false;
      this.statisticsSummary = data;
    })
  }


}
