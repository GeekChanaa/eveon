import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { AtomsModule } from 'src/app/atoms/atoms.module';
import { SharedModule } from 'src/app/shared/shared.module';
import { SystemReportsComponent } from './system-reports.component';
import { SystemReportsListComponent } from './system-reports-list/system-reports-list.component';
import { CreateSystemReportComponent } from './create-system-report/create-system-report.component';
import { SystemReportComponent } from './system-report/system-report.component';
import { SystemReportsRoutingModule } from './system-reports-routing.module';

@NgModule({
    declarations: [
      SystemReportsComponent,
      SystemReportsListComponent,
      CreateSystemReportComponent,
      SystemReportComponent
    ],
    imports: [
        AtomsModule,
        ReactiveFormsModule,
        CommonModule,
        SharedModule,
        FormsModule,
        SystemReportsRoutingModule
    ],
  })
  export class SystemReportsModule { }
  