import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { AtomsModule } from 'src/app/atoms/atoms.module';
import { SharedModule } from 'src/app/shared/shared.module';
import { ChargingSessionsListComponent } from './charging-sessions-list/charging-sessions-list.component';
import { ChargingSessionComponent } from './charging-session/charging-session.component';
import { ChargingSessionsRoutingModule } from './charging-sessions-routing.module';
@NgModule({
    declarations: [
      ChargingSessionsListComponent,
      ChargingSessionComponent
    ],
    imports: [
        AtomsModule,
        ReactiveFormsModule,
        CommonModule,
        SharedModule,
        FormsModule,
        ChargingSessionsRoutingModule
    ],
  })
  export class ChargingSessionsModule { }
  