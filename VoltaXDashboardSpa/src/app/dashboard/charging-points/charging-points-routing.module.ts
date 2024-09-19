import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CreateChargePointComponent } from './create-charge-point/create-charge-point.component';
import { ChargePointsListComponent } from './charge-points-list/charge-points-list.component';
import { ChargingPointComponent } from './charging-point/charging-point.component';
const routes: Routes = [
  {
    path: "",
    component: ChargePointsListComponent
  },
  {
    path: "create",
    component: CreateChargePointComponent
  },
  {
    path: ":id",
    component: ChargingPointComponent
  },
  
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ChargingPointsRoutingModule { }
