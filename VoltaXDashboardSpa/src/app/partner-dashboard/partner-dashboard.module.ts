import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { NgApexchartsModule } from 'ng-apexcharts';
import { AtomsModule } from '../atoms/atoms.module';
import { SharedModule } from '../shared/shared.module';
import { CommonModule } from '@angular/common';
import { MatRippleModule } from '@angular/material/core';
import { PartnerDashboardRoutingModule } from './partner-dashboard-routing.module';
@NgModule({
    declarations: [
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
  