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

@NgModule({
    declarations: [
      ConnectorRealtimeComponent,
      ConnectorRealtimeListComponent,
      ConnectorRealtimeActionsComponent,
      AppTableCustomButtonDirective,
      RequestHandlerComponent,
      ConnectorRealtimeDetailsSettingsComponent,
      ConnectorRealtimeFirmwareComponent,
      ConnectorRealtimeLogsComponent,
      ConnectorRealtimeMainComponent,
      ConnectorRealtimeOcppConfigurationComponent,
      ConnectorRealtimeOverviewComponent,
      ConnectorRealtimePricingAccessComponent,
      ConnectorRealtimeUptimeReportsComponent,
      ConnectorRealtimeRatingsComponent
  ],
    imports: [
        AtomsModule,
        ReactiveFormsModule,
        CommonModule,
        SharedModule,
        FormsModule,
        ConnectorRealtimeRoutingModule
    ],
  })
  export class ConnectorRealtimeModule { }
  