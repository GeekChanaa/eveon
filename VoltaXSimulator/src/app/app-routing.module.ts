import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ChargingStationsComponent } from './charging-stations/charging-stations.component';
import { ChargePointsComponent } from './charge-points/charge-points.component';
import { ChargePointSimComponent } from './charge-point-sim/charge-point-sim.component';

const routes: Routes = [
  {
    path : 'charging-stations',
    component : ChargingStationsComponent
  },{
    path : 'charge-points',
    component : ChargePointsComponent
  },
  { path: 'chargepoint/:id', component: ChargePointSimComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
