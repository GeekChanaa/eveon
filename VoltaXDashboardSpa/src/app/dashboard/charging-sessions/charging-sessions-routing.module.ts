import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ChargingSessionDetailComponent } from './charging-session-detail/charging-session-detail.component';
import { ChargingSessionsListComponent } from './charging-sessions-list/charging-sessions-list.component';
const routes: Routes = [
  {
    path: "",
    component: ChargingSessionsListComponent
  },
  {
    path: ":id",
    component: ChargingSessionDetailComponent
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ChargingSessionsRoutingModule { }
