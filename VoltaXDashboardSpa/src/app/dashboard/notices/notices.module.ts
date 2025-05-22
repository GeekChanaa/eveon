import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { AtomsModule } from 'src/app/atoms/atoms.module';
import { SharedModule } from 'src/app/shared/shared.module';
import { NoticesRoutingModule } from './notices-routing.module';
import { NoticesComponent } from './notices.component';
import { NoticesListComponent } from './notices-list/notices-list.component';
import { CreateNoticeComponent } from './create-notice/create-notice.component';
import { NoticeComponent } from './notice/notice.component';

@NgModule({
    declarations: [
        NoticesComponent,
        NoticesListComponent,
        CreateNoticeComponent,
        NoticeComponent
    ],
    imports: [
        AtomsModule,
        ReactiveFormsModule,
        CommonModule,
        SharedModule,
        FormsModule,
        NoticesRoutingModule
    ],
  })
  export class NoticesModule { }
  