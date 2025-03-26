import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { PartnersListComponent } from './partners-list/partners-list.component';
import { PartnerComponent } from './partner/partner.component';
import { CreatePartnerComponent } from './create-partner/create-partner.component';
import { AddPartnerLogoComponent } from './create-partner/add-partner-logo/add-partner-logo.component';
const routes: Routes = [
  {
    path: "",
    component: PartnersListComponent
  },
  {
    path: "create",
    component: CreatePartnerComponent
  },
  {
    path: "add-partner-logo/:id",
    component: AddPartnerLogoComponent
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
