import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ReportsListComponent } from './reports-list/reports-list.component';
import { CreateReportComponent } from './create-report/create-report.component';
import { ReportComponent } from './report/report.component';
const routes: Routes = [
  {
    path: "",
    component: ReportsListComponent
  },
  {
    path: "create",
    component: CreateReportComponent
  },
  {
    path: ":id",
    component: ReportComponent
  },
  
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ReportsRoutingModule { }
