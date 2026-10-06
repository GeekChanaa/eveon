import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ChargePointConfigurationsListComponent } from './charge-point-configurations-list/charge-point-configurations-list.component';
import { ChargePointDeviceModelComponent } from './charge-point-device-model/charge-point-device-model.component';
import { ChargePointSetupComponent } from './charge-point-setup/charge-point-setup.component';

const routes: Routes = [
  {
    path: "",
    component: ChargePointConfigurationsListComponent
  },
  {
    path: ":chargePointId",
    component: ChargePointDeviceModelComponent
  },
  {
    path: ":chargePointId/setup",
    component: ChargePointSetupComponent
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ChargePointConfigurationsRoutingModule { }
