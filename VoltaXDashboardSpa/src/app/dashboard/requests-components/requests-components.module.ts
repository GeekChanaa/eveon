import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { AtomsModule } from 'src/app/atoms/atoms.module';
import { SharedModule } from 'src/app/shared/shared.module';
import { RequestStopTransactionComponent } from './request-stop-transaction/request-stop-transaction.component';

@NgModule({
    declarations: [
      RequestStopTransactionComponent
    ],
    imports: [
        AtomsModule,
        ReactiveFormsModule,
        CommonModule,
        SharedModule,
        FormsModule
    ],
    exports: [
      RequestStopTransactionComponent
    ]
  })
  export class RequestsComponentsModule { }
  