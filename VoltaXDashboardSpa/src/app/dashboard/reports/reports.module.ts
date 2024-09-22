import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { AtomsModule } from 'src/app/atoms/atoms.module';
import { SharedModule } from 'src/app/shared/shared.module';
import { ReportsListComponent } from './reports-list/reports-list.component';
import { ReportsComponent } from './reports.component';
import { ReportsRoutingModule } from './reports-routing.module';
import { CreateReportComponent } from './create-report/create-report.component';
import { ReportComponent } from './report/report.component';


@NgModule({
  declarations: [
    ReportsListComponent,
    ReportsComponent,
    CreateReportComponent,
    ReportComponent
  ],
  imports: [
      AtomsModule,
      ReactiveFormsModule,
      CommonModule,
      SharedModule,
      ReportsRoutingModule,
      FormsModule,
  ],
})
export class ReportsModule { }
  