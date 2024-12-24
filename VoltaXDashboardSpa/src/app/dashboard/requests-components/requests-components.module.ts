import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { AtomsModule } from 'src/app/atoms/atoms.module';
import { SharedModule } from 'src/app/shared/shared.module';
import { RequestStopTransactionComponent } from './request-stop-transaction/request-stop-transaction.component';
import { RequestStartTransactionComponent } from './request-start-transaction/request-start-transaction.component';
import { RequestUnlockConnectorComponent } from './request-unlock-connector/request-unlock-connector.component';
import { RequestGetBaseReportComponent } from './request-get-base-report/request-get-base-report.component';

@NgModule({
    declarations: [
      RequestStopTransactionComponent,
      RequestStartTransactionComponent,
      RequestUnlockConnectorComponent,
      RequestGetBaseReportComponent
    ],
    imports: [
        AtomsModule,
        ReactiveFormsModule,
        CommonModule,
        SharedModule,
        FormsModule
    ],
    exports: [
      RequestStopTransactionComponent,
      RequestStartTransactionComponent,
      RequestUnlockConnectorComponent,
      RequestGetBaseReportComponent
    ]
  })
  export class RequestsComponentsModule { }
  