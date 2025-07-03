import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CustomerHomeComponent } from './customer-home/customer-home.component';
import { ProfileComponent } from '../dashboard/profile/profile.component';
const routes: Routes = [
  {
    path: "",
    component: CustomerHomeComponent,
  },{
    path: "profile",
    component: ProfileComponent,
  }
  
  
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class CustomerDashboardRoutingModule { }
