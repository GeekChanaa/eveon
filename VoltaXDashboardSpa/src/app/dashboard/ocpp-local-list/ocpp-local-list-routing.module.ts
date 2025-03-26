import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { OcppLocalListChargePointsComponent } from './ocpp-local-list-charge-points/ocpp-local-list-charge-points.component';
const routes: Routes = [
  {
    path: "",
    component: OcppLocalListChargePointsComponent
  },
  
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class OcppLocalListRoutingModule { }
