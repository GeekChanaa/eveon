import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { AtomsModule } from 'src/app/atoms/atoms.module';
import { SharedModule } from 'src/app/shared/shared.module';
import { ChargingSessionsListComponent } from './charging-sessions-list/charging-sessions-list.component';
import { ChargingSessionDetailModule } from './charging-session-detail/charging-session-detail.module';
import { ChargingSessionsRoutingModule } from './charging-sessions-routing.module';
import { PortalUiModule } from 'src/app/shared/portal-ui/portal-ui.module';
@NgModule({
    declarations: [
      ChargingSessionsListComponent
    ],
    imports: [
        AtomsModule,
        ReactiveFormsModule,
        CommonModule,
        SharedModule,
        PortalUiModule,
        FormsModule,
        ChargingSessionDetailModule,
        ChargingSessionsRoutingModule
    ],
  })
  export class ChargingSessionsModule { }
  