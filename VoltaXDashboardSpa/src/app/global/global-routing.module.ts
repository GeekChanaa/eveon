import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { RechargeCardOrderComponent } from './recharge-card-order/recharge-card-order.component';
const routes: Routes = [
  {
    path: "checkout",
    component: RechargeCardOrderComponent,
  },
  
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class GlobalRoutingModule { }
