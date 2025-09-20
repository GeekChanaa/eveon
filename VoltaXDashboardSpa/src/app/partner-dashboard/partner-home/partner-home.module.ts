import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { AtomsModule } from 'src/app/atoms/atoms.module';
import { SharedModule } from 'src/app/shared/shared.module';
import { PartnerHomeConnectorStatusesComponent } from './partner-home-connector-statuses/partner-home-connector-statuses.component';
import { PartnerHomeComponent } from './partner-home.component';
import { PartnersTabStatisticsComponent } from './partners-tab-statistics/partners-tab-statistics.component';
import { NgApexchartsModule } from 'ng-apexcharts';


@NgModule({
  declarations: [
    PartnerHomeConnectorStatusesComponent,
    PartnerHomeComponent,
    PartnersTabStatisticsComponent
  ],
  imports: [
      AtomsModule,
      ReactiveFormsModule,
      CommonModule,
      SharedModule,
      FormsModule,
      NgApexchartsModule
  ],
})
export class PartnerHomeModule { }
  