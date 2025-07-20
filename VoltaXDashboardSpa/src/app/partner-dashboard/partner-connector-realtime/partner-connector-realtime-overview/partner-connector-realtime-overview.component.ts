import { Component, Input, OnInit } from '@angular/core';
import { ChargePointStatisticsSummaryDto } from 'src/_models/_dtos/statistics-dtos/charge-point-summary-statistics-dto';
import { StatisticsService } from 'src/_services/statistics.service';

@Component({
  selector: 'app-partner-connector-realtime-overview',
  templateUrl: './partner-connector-realtime-overview.component.html',
  styleUrls: ['./partner-connector-realtime-overview.component.sass']
})
export class PartnerConnectorRealtimeOverviewComponent implements OnInit {

  
    @Input() chargePoint : any = {}
  
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
      this._statisticsService.getChargePointStatisticsSummary(this.chargePoint.id).subscribe((data) => {
        this.summaryIsLoading = false;
        this.statisticsSummary = data;
      })
    }
  
  

}
