import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { RechargeCardOrderComponent } from './recharge-card-order/recharge-card-order.component';
import { ServerErrorComponent } from './error-pages/server-error/server-error.component';
import { NotAuthorizedComponent } from './error-pages/not-authorized/not-authorized.component';
import { NotFoundComponent } from './error-pages/not-found/not-found.component';
const routes: Routes = [
  {
    path: "checkout",
    component: RechargeCardOrderComponent,
  },
  {
    path: "not-found",
    component: NotFoundComponent,
  },
  {
    path: "unauthorized",
    component: NotAuthorizedComponent,
  },
  {
    path: "server-error",
    component: ServerErrorComponent,
  },
  
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class GlobalRoutingModule { }
