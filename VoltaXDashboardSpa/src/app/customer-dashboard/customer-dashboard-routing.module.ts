import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CustomerHomeComponent } from './customer-home/customer-home.component';
import { MyCardsComponent } from './my-cards/my-cards.component';
import { ProfileComponent } from '../dashboard/profile/profile.component';
const routes: Routes = [
  {
    path: "",
    component: CustomerHomeComponent,
  },{
    path: "my-cards",
    component: MyCardsComponent,
  },{
    path: "my-profile",
    component: ProfileComponent,
  },
  
  
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class CustomerDashboardRoutingModule { }
