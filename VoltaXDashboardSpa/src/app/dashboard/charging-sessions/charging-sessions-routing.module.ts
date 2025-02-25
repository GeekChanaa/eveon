import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ChargingSessionComponent } from './charging-session/charging-session.component';
import { ChargingSessionsListComponent } from './charging-sessions-list/charging-sessions-list.component';
const routes: Routes = [
  {
    path: "",
    component: ChargingSessionsListComponent
  },
  {
    path: ":id",
    component: ChargingSessionComponent
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ChargingSessionsRoutingModule { }
