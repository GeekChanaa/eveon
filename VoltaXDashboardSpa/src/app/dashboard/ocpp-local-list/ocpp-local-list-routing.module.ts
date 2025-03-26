import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { OcppLocalListChargePointsComponent } from './ocpp-local-list-charge-points/ocpp-local-list-charge-points.component';
import { OcppLocalListChargePointComponent } from './ocpp-local-list-charge-point/ocpp-local-list-charge-point.component';
const routes: Routes = [
  {
    path: "",
    component: OcppLocalListChargePointsComponent
  },
  {
    path: "charge-point/:id",
    component: OcppLocalListChargePointComponent
  },
  
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class OcppLocalListRoutingModule { }
