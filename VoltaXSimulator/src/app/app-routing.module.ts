import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ChargingStationsComponent } from './charging-stations/charging-stations.component';

const routes: Routes = [
  {
    path : 'charging-stations',
    component : ChargingStationsComponent
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
