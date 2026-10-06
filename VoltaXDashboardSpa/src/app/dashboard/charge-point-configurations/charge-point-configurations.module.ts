import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AtomsModule } from 'src/app/atoms/atoms.module';
import { ChargePointConfigurationsRoutingModule } from './charge-point-configurations-routing.module';
import { ChargePointConfigurationsListComponent } from './charge-point-configurations-list/charge-point-configurations-list.component';
import { ChargePointDeviceModelComponent } from './charge-point-device-model/charge-point-device-model.component';
import { ChargePointSetupComponent } from './charge-point-setup/charge-point-setup.component';
import { ChargePointOcpp16ConfigurationComponent } from './charge-point-ocpp16-configuration/charge-point-ocpp16-configuration.component';
import { OcppVersionBadgeComponent } from 'src/app/atoms/ocpp-version-badge/ocpp-version-badge.component';

@NgModule({
  declarations: [
    ChargePointConfigurationsListComponent,
    ChargePointDeviceModelComponent,
    ChargePointSetupComponent,
    ChargePointOcpp16ConfigurationComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    AtomsModule,
    OcppVersionBadgeComponent,
    ChargePointConfigurationsRoutingModule
  ]
})
export class ChargePointConfigurationsModule { }
