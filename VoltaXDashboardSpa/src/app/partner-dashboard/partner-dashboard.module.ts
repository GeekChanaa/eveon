import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { NgApexchartsModule } from 'ng-apexcharts';
import { AtomsModule } from '../atoms/atoms.module';
import { SharedModule } from '../shared/shared.module';
import { CommonModule } from '@angular/common';
import { MatRippleModule } from '@angular/material/core';
import { PartnerDashboardRoutingModule } from './partner-dashboard-routing.module';
import { PartnerChargePointsComponent } from './partner-charge-points/partner-charge-points.component';
import { PartnerChargingStationsComponent } from './partner-charging-stations/partner-charging-stations.component';
import { PartnerConnectorsComponent } from './partner-connectors/partner-connectors.component';
import { PartnerHomeComponent } from './partner-home/partner-home.component';
import { PartnerStatisticsComponent } from './partner-statistics/partner-statistics.component';
@NgModule({
    declarations: [
        PartnerChargePointsComponent,
        PartnerChargingStationsComponent,
        PartnerConnectorsComponent,
        PartnerHomeComponent,
        PartnerStatisticsComponent
    ],
    imports: [
        AtomsModule,
        NgApexchartsModule,
        ReactiveFormsModule,
        CommonModule,
        SharedModule,
        FormsModule,
        PartnerDashboardRoutingModule
    ],
  })
  export class PartnerDashboardModule { }
  