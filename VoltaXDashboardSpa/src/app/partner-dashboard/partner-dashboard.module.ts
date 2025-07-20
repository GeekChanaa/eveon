import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { NgApexchartsModule } from 'ng-apexcharts';
import { AtomsModule } from '../atoms/atoms.module';
import { SharedModule } from '../shared/shared.module';
import { CommonModule } from '@angular/common';
import { PartnerDashboardRoutingModule } from './partner-dashboard-routing.module';
import { PartnerStatisticsComponent } from './partner-statistics/partner-statistics.component';
@NgModule({
    declarations: [
        PartnerStatisticsComponent,
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
  