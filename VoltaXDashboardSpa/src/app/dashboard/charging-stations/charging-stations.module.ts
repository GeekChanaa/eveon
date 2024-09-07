import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { AtomsModule } from 'src/app/atoms/atoms.module';
import { SharedModule } from 'src/app/shared/shared.module';
import { ChargingStationComponent } from './charging-station/charging-station.component';
import { ChargingStationChargePointComponent } from './charging-station-charge-point/charging-station-charge-point.component';
import { ChargingStationChargePointsComponent } from './charging-station-charge-points/charging-station-charge-points.component';
import { CreateChargingStationComponent } from './create-charging-station/create-charging-station.component';
import { ChargingStationAddChargePointComponent } from './charging-station-add-charge-point/charging-station-add-charge-point.component';
import { ChargingStationRoutingModule } from './charging-station-routing.module';
import { ChargingStationsComponent } from './charging-stations.component';
import { ChargingStationsListComponent } from './charging-stations-list/charging-stations-list.component';

@NgModule({
    declarations: [
    ChargingStationComponent,
    ChargingStationChargePointComponent,
    ChargingStationChargePointsComponent,
    CreateChargingStationComponent,
    ChargingStationAddChargePointComponent,
    ChargingStationsComponent,
    ChargingStationsListComponent
  ],
    imports: [
        AtomsModule,
        ReactiveFormsModule,
        CommonModule,
        SharedModule,
        FormsModule,
        ChargingStationRoutingModule
    ],
  })
  export class ChargingStationsModule { }
  