import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ChargingStationsComponent } from './charging-stations.component';
import { CreateChargingStationComponent } from './create-charging-station/create-charging-station.component';
import { ChargingStationComponent } from './charging-station/charging-station.component';
import { ChargingStationsListComponent } from './charging-stations-list/charging-stations-list.component';
const routes: Routes = [
  {
    path: "",
    component: ChargingStationsListComponent
  },
  {
    path: "create",
    component: CreateChargingStationComponent
  },
  {
    path: ":id",
    component: ChargingStationComponent
  },
  
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ChargingStationRoutingModule { }
