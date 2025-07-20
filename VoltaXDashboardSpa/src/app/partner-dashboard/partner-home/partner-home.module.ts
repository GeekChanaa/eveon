import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { AtomsModule } from 'src/app/atoms/atoms.module';
import { SharedModule } from 'src/app/shared/shared.module';
import { PartnerHomeConnectorStatusesComponent } from './partner-home-connector-statuses/partner-home-connector-statuses.component';
import { PartnerHomeComponent } from './partner-home.component';


@NgModule({
  declarations: [
    PartnerHomeConnectorStatusesComponent,
    PartnerHomeComponent
  ],
  imports: [
      AtomsModule,
      ReactiveFormsModule,
      CommonModule,
      SharedModule,
      FormsModule,
  ],
})
export class PartnerHomeModule { }
  