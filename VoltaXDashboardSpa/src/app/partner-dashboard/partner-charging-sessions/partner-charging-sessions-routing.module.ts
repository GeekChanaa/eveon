import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { PartnerChargingSessionsComponent } from './partner-charging-sessions.component';
import { PartnerChargingSessionsListComponent } from './partner-charging-sessions-list/partner-charging-sessions-list.component';
const routes: Routes = [
  {
    path: "",
    component: PartnerChargingSessionsListComponent
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class PartnerChargingSessionsRoutingModule { }
