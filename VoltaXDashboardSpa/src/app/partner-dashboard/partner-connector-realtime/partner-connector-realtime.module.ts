import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { NgApexchartsModule } from 'ng-apexcharts';
import { AtomsModule } from 'src/app/atoms/atoms.module';
import { SharedModule } from 'src/app/shared/shared.module';
import { PartnerConnectorRealtimeRoutingModule } from './partner-connector-realtime-routing.module';
import { PartnerConnectorRealtimeChargingSessionsComponent } from './partner-connector-realtime-charging-sessions/partner-connector-realtime-charging-sessions.component';
import { PartnerConnectorRealtimeDetailsSettingsComponent } from './partner-connector-realtime-details-settings/partner-connector-realtime-details-settings.component';
import { PartnerConnectorRealtimeListComponent } from './partner-connector-realtime-list/partner-connector-realtime-list.component';
import { PartnerConnectorRealtimeMainOverviewComponent } from './partner-connector-realtime-main-overview/partner-connector-realtime-main-overview.component';
import { PartnerConnectorRealtimeMainComponent } from './partner-connector-realtime-main/partner-connector-realtime-main.component';
import { PartnerConnectorRealtimeOverviewComponent } from './partner-connector-realtime-overview/partner-connector-realtime-overview.component';
import { PartnerConnectorRealtimeUptimeReportsComponent } from './partner-connector-realtime-uptime-reports/partner-connector-realtime-uptime-reports.component';
import { PartnerConnectorRealtimeComponent } from './partner-connector-realtime.component';

@NgModule({
    declarations: [
        PartnerConnectorRealtimeComponent,
        PartnerConnectorRealtimeListComponent,
        PartnerConnectorRealtimeDetailsSettingsComponent,
        PartnerConnectorRealtimeMainComponent,
        PartnerConnectorRealtimeOverviewComponent,
        PartnerConnectorRealtimeUptimeReportsComponent,  
        PartnerConnectorRealtimeChargingSessionsComponent,
        PartnerConnectorRealtimeMainOverviewComponent,
    ],
    imports: [
        AtomsModule,
        ReactiveFormsModule,
        CommonModule,
        SharedModule,
        FormsModule,
        PartnerConnectorRealtimeRoutingModule,
        NgApexchartsModule
    ],
  })
  export class PartnerConnectorRealtimeModule { }
  