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
import { ChargingStationImagesComponent } from './charging-station-images/charging-station-images.component';
import { CreateChargingStationTypeChoiceComponent } from './create-charging-station/create-charging-station-type-choice/create-charging-station-type-choice.component';
import { CreateChargingStationAddChargePointsComponent } from './create-charging-station/create-charging-station-add-charge-points/create-charging-station-add-charge-points.component';
import { CreateChargingStationAddressComponent } from './create-charging-station/create-charging-station-address/create-charging-station-address.component';
import { CreateChargingStationInformationsComponent } from './create-charging-station/create-charging-station-informations/create-charging-station-informations.component';
import { CreateChargingStationAddChargePointModalComponent } from './create-charging-station/create-charging-station-add-charge-points/create-charging-station-add-charge-point-modal/create-charging-station-add-charge-point-modal.component';
import { CreateChargingStationAddConnectorModalComponent } from './create-charging-station/create-charging-station-add-charge-points/create-charging-station-add-connector-modal/create-charging-station-add-connector-modal.component';
import { CreateChargingStationPreviewChargePointsComponent } from './create-charging-station/create-charging-station-add-charge-points/create-charging-station-preview-charge-points/create-charging-station-preview-charge-points.component';
import { CreateChargingStationImagesComponent } from './create-charging-station/create-charging-station-images/create-charging-station-images.component';
import { CreateChargingStationPreviewComponent } from './create-charging-station/create-charging-station-preview/create-charging-station-preview.component';

@NgModule({
    declarations: [
    ChargingStationComponent,
    ChargingStationChargePointComponent,
    ChargingStationChargePointsComponent,
    CreateChargingStationComponent,
    ChargingStationAddChargePointComponent,
    ChargingStationsComponent,
    ChargingStationsListComponent,
    ChargingStationImagesComponent,
    CreateChargingStationTypeChoiceComponent,
    CreateChargingStationAddChargePointsComponent,
    CreateChargingStationAddressComponent,
    CreateChargingStationInformationsComponent,
    CreateChargingStationAddChargePointModalComponent,
    CreateChargingStationAddConnectorModalComponent,
    CreateChargingStationPreviewChargePointsComponent,
    CreateChargingStationImagesComponent,
    CreateChargingStationPreviewComponent
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
  