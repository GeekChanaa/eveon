import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { RechargeOrdersListComponent } from './recharge-orders-list/recharge-orders-list.component';
import { CreateRechargeOrderComponent } from './create-recharge-order/create-recharge-order.component';
import { RechargeOrderComponent } from './recharge-order/recharge-order.component';
const routes: Routes = [
  {
    path: "",
    component: RechargeOrdersListComponent
  },
  {
    path: "create",
    component: CreateRechargeOrderComponent
  },
  {
    path: ":id",
    component: RechargeOrderComponent
  },
  
  
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class RechargeOrdersRoutingModule { }
