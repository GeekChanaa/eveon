import { ChargePointStatusComponent } from 'src/app/atoms/charge-point-status/charge-point-status.component';
import { OcppVersionBadgeComponent } from 'src/app/atoms/ocpp-version-badge/ocpp-version-badge.component';
import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { AtomsModule } from 'src/app/atoms/atoms.module';
import { SharedModule } from 'src/app/shared/shared.module';
import { ConnectorRealtimeRoutingModule } from './connector-realtime-routing.module';
import { ConnectorRealtimeComponent } from './connector-realtime.component';
import { ConnectorRealtimeListComponent } from './connector-realtime-list/connector-realtime-list.component';
import { ConnectorRealtimeActionsComponent } from './connector-realtime-actions/connector-realtime-actions.component';
import { AppTableCustomButtonDirective } from 'src/_directives/table-custom-button.directive';
import { RequestHandlerComponent } from './connector-realtime-actions/request-handler/request-handler.component';
import { ConnectorRealtimeDetailsSettingsComponent } from './connector-realtime-details-settings/connector-realtime-details-settings.component';
import { ConnectorRealtimeFirmwareComponent } from './connector-realtime-firmware/connector-realtime-firmware.component';
import { ConnectorRealtimeLogsComponent } from './connector-realtime-logs/connector-realtime-logs.component';
import { ConnectorRealtimeMainComponent } from './connector-realtime-main/connector-realtime-main.component';
import { ConnectorRealtimeOcppConfigurationComponent } from './connector-realtime-ocpp-configuration/connector-realtime-ocpp-configuration.component';
import { ConnectorRealtimeOverviewComponent } from './connector-realtime-overview/connector-realtime-overview.component';
import { ConnectorRealtimePricingAccessComponent } from './connector-realtime-pricing-access/connector-realtime-pricing-access.component';
import { ConnectorRealtimeUptimeReportsComponent } from './connector-realtime-uptime-reports/connector-realtime-uptime-reports.component';
import { ConnectorRealtimeRatingsComponent } from './connector-realtime-ratings/connector-realtime-ratings.component';
import { ConnectorRealtimeChargingSessionsComponent } from './connector-realtime-charging-sessions/connector-realtime-charging-sessions.component';
import { ConnectorRealtimeMainOverviewComponent } from './connector-realtime-main-overview/connector-realtime-main-overview.component';
import { ConnectorRealtimeTransactionsComponent } from './connector-realtime-transactions/connector-realtime-transactions.component';
import { RequestsComponentsModule } from '../requests-components/requests-components.module';
import { ConnectorRealtimeMessageLogComponent } from './connector-realtime-logs/connector-realtime-message-log/connector-realtime-message-log.component';
import { ConnectorRealtimeConfigurationsComponent } from './connector-realtime-configurations/connector-realtime-configurations.component';
import { ConnectorRealtimeChargingSessionInformationsComponent } from './connector-realtime-charging-session-informations/connector-realtime-charging-session-informations.component';
import { ConnectorRealtimeOverviewStatisticsComponent } from './connector-realtime-overview/connector-realtime-overview-statistics/connector-realtime-overview-statistics.component';
import { NgApexchartsModule } from 'ng-apexcharts';
import { ConnectorRealtimeRatingsReportComponent } from './connector-realtime-ratings/connector-realtime-ratings-report/connector-realtime-ratings-report.component';

@NgModule({
    declarations: [
      ConnectorRealtimeComponent,
      ConnectorRealtimeListComponent,
      ConnectorRealtimeActionsComponent,
      RequestHandlerComponent,
      ConnectorRealtimeDetailsSettingsComponent,
      ConnectorRealtimeFirmwareComponent,
      ConnectorRealtimeLogsComponent,
      ConnectorRealtimeMainComponent,
      ConnectorRealtimeOcppConfigurationComponent,
      ConnectorRealtimeOverviewComponent,
      ConnectorRealtimePricingAccessComponent,
      ConnectorRealtimeUptimeReportsComponent,  
      ConnectorRealtimeRatingsComponent,
      ConnectorRealtimeChargingSessionsComponent,
      ConnectorRealtimeMainOverviewComponent,
      ConnectorRealtimeTransactionsComponent,
      ConnectorRealtimeMessageLogComponent,
      ConnectorRealtimeConfigurationsComponent,
      ConnectorRealtimeChargingSessionInformationsComponent,
      ConnectorRealtimeOverviewStatisticsComponent,
      ConnectorRealtimeRatingsReportComponent
  ],
    imports: [
        ChargePointStatusComponent,
        OcppVersionBadgeComponent,
        AtomsModule,
        ReactiveFormsModule,
        CommonModule,
        SharedModule,
        FormsModule,
        ConnectorRealtimeRoutingModule,
        RequestsComponentsModule,
        NgApexchartsModule
    ],
  })
  export class ConnectorRealtimeModule { }
  