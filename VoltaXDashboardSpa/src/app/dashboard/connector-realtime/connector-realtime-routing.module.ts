import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ConnectorRealtimeListComponent } from './connector-realtime-list/connector-realtime-list.component';
import { ConnectorRealtimeActionsComponent } from './connector-realtime-actions/connector-realtime-actions.component';
import { ConnectorRealtimeMainComponent } from './connector-realtime-main/connector-realtime-main.component';
import { ChargingSessionDetailComponent } from '../charging-sessions/charging-session-detail/charging-session-detail.component';
const routes: Routes = [
  {
    path: "",
    component: ConnectorRealtimeListComponent
  },
  {
    path: ":id",
    component: ConnectorRealtimeMainComponent
  },
  {
    path: ":chargePointID/charging-session/:id",
    component: ChargingSessionDetailComponent
  }
  
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ConnectorRealtimeRoutingModule { }
