import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ConnectorRealtimeListComponent } from './connector-realtime-list/connector-realtime-list.component';
import { ConnectorRealtimeActionsComponent } from './connector-realtime-actions/connector-realtime-actions.component';
const routes: Routes = [
  {
    path: "",
    component: ConnectorRealtimeListComponent
  },
  {
    path: ":id",
    component: ConnectorRealtimeActionsComponent
  }
  
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ConnectorRealtimeRoutingModule { }
