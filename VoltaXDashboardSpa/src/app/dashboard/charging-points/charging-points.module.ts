import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { AtomsModule } from 'src/app/atoms/atoms.module';
import { SharedModule } from 'src/app/shared/shared.module';
import { ChargingPointsRoutingModule } from './charging-points-routing.module';
import { ChargePointsListComponent } from './charge-points-list/charge-points-list.component';
import { CreateChargePointComponent } from './create-charge-point/create-charge-point.component';
import { ChargePointConnectorsComponent } from './charging-point/charge-point-connectors/charge-point-connectors.component';
import { ChargingPointsComponent } from './charging-points.component';
import { ChargingPointComponent } from './charging-point/charging-point.component';
import { ChargePointConnectorComponent } from './charge-point-connector/charge-point-connector.component';
import { ChargePointAddConnectorComponent } from './charge-point-add-connector/charge-point-add-connector.component';
import { ChargePointEditConnectorComponent } from './charge-point-edit-connector/charge-point-edit-connector.component';

@NgModule({
    declarations: [
      ChargePointsListComponent,
      CreateChargePointComponent,
      ChargePointConnectorsComponent,
      ChargingPointsComponent,
      ChargingPointComponent,
      ChargePointConnectorComponent,
      ChargePointAddConnectorComponent,
      ChargePointEditConnectorComponent
    ],
    imports: [
        AtomsModule,
        ReactiveFormsModule,
        CommonModule,
        SharedModule,
        FormsModule,
        ChargingPointsRoutingModule
    ],
  })
  export class ChargingPointsModule { }
  