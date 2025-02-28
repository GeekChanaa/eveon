import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CreateSystemReportComponent } from './create-system-report/create-system-report.component';
import { SystemReportComponent } from './system-report/system-report.component';
import { SystemReportsListComponent } from './system-reports-list/system-reports-list.component';
import { SystemReportEditComponent } from './system-report-edit/system-report-edit.component';
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
    path: "edit/:id",
    component: SystemReportEditComponent
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
