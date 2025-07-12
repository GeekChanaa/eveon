import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { PartnerHomeComponent } from '../partner-home/partner-home.component';
import { PartnerConnectorRealtimeListComponent } from './partner-connector-realtime-list/partner-connector-realtime-list.component';
import { PartnerConnectorRealtimeMainComponent } from './partner-connector-realtime-main/partner-connector-realtime-main.component';
const routes: Routes = [
  {
      path: "",
      component: PartnerConnectorRealtimeListComponent
    },
    {
      path: ":id",
      component: PartnerConnectorRealtimeMainComponent
    }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class PartnerConnectorRealtimeRoutingModule { }
