import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { PartnerHomeComponent } from './partner-home/partner-home.component';
import { PartnerChargingStationsComponent } from './partner-charging-stations/partner-charging-stations.component';
import { PartnerChargePointsComponent } from './partner-charge-points/partner-charge-points.component';
import { PartnerConnectorsComponent } from './partner-connectors/partner-connectors.component';
import { PartnerStatisticsComponent } from './partner-statistics/partner-statistics.component';
import { PartnerTransactionsComponent } from './partner-transactions/partner-transactions.component';
const routes: Routes = [
  {
    path: "",
    component: PartnerHomeComponent,
  },{
    path: "charging-stations",
    component: PartnerChargingStationsComponent,
  },{
    path: "charge-points",
    component: PartnerChargePointsComponent,
  },{
    path: "connectors",
    component: PartnerConnectorsComponent,
  },{
    path: "statistics",
    component: PartnerStatisticsComponent,
  },{
    path: "transactions",
    component: PartnerTransactionsComponent,
  }
  
  
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class PartnerDashboardRoutingModule { }
