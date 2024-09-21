import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ChargingCardsListComponent } from './charging-cards-list/charging-cards-list.component';
const routes: Routes = [
  {
    path: "",
    component: ChargingCardsListComponent
  },
  
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ChargingCardsRoutingModule { }
