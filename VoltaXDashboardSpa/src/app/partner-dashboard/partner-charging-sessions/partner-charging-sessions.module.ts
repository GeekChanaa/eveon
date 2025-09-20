import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { AtomsModule } from 'src/app/atoms/atoms.module';
import { SharedModule } from 'src/app/shared/shared.module';
import { PartnerChargingSessionsRoutingModule } from './partner-charging-sessions-routing.module';
import { PartnerChargingSessionsListComponent } from './partner-charging-sessions-list/partner-charging-sessions-list.component';
import { PartnerChargingSessionsComponent } from './partner-charging-sessions.component';
@NgModule({
    declarations: [
        PartnerChargingSessionsListComponent,
        PartnerChargingSessionsComponent
    ],
    imports: [
        AtomsModule,
        ReactiveFormsModule,
        CommonModule,
        SharedModule,
        FormsModule,
        PartnerChargingSessionsRoutingModule
    ],
  })
  export class PartnerChargingSessionsModule { }
  