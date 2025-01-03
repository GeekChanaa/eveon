import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CreateSystemReportComponent } from './create-system-report/create-system-report.component';
import { SystemReportComponent } from './system-report/system-report.component';
import { SystemReportsListComponent } from './system-reports-list/system-reports-list.component';
const routes: Routes = [
  {
    path: "",
    component: SystemReportsListComponent
  },
  {
    path: "create",
    component: CreateSystemReportComponent
  },
  {
    path: ":id",
    component: SystemReportComponent
  },
  
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class SystemReportsRoutingModule { }
