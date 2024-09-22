import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { PartnersListComponent } from './partners-list/partners-list.component';
import { PartnerComponent } from './partner/partner.component';
const routes: Routes = [
  {
    path: "",
    component: PartnersListComponent
  },
  {
    path: ":id",
    component: PartnerComponent
  },
  
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class PartnersRoutingModule { }
