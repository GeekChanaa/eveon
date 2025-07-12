import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { PartnerHomeComponent } from './partner-home/partner-home.component';
import { PartnerConnectorRealtimeComponent } from './partner-connector-realtime/partner-connector-realtime.component';
const routes: Routes = [
  {
    path: "",
    component: PartnerHomeComponent,
  },
  {
    path: "connector-realtime",
    component: PartnerConnectorRealtimeComponent,
    loadChildren : () => import('./partner-connector-realtime/partner-connector-realtime.module')
      .then(m=>m.PartnerConnectorRealtimeModule)
  },
  
  
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class PartnerDashboardRoutingModule { }
