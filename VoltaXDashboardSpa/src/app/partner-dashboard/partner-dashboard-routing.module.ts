import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { PartnerHomeComponent } from './partner-home/partner-home.component';
import { PartnerConnectorRealtimeComponent } from './partner-connector-realtime/partner-connector-realtime.component';
import { StatisticsComponent } from '../dashboard/statistics/statistics.component';
import { PartnerStatisticsComponent } from './partner-statistics/partner-statistics.component';
const routes: Routes = [
  {
    path: "",
    component: PartnerHomeComponent,
    loadChildren : () => import('./partner-home/partner-home.module')
      .then(m=>m.PartnerHomeModule)
  },
  {
    path: "statistics",
    component: PartnerStatisticsComponent
  },
  {
    path: "connector-realtime",
    component: PartnerConnectorRealtimeComponent,
    loadChildren : () => import('./partner-connector-realtime/partner-connector-realtime.module')
      .then(m=>m.PartnerConnectorRealtimeModule)
  },
  {
    path: "charging-sessions",
    component: PartnerConnectorRealtimeComponent,
    loadChildren : () => import('./partner-charging-sessions/partner-charging-sessions.module')
      .then(m=>m.PartnerChargingSessionsModule)
  },
  
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class PartnerDashboardRoutingModule { }
