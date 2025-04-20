import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { AtomsModule } from 'src/app/atoms/atoms.module';
import { SharedModule } from 'src/app/shared/shared.module';
import { UserInfoDownloadRequestsComponent } from './user-info-download-requests.component';
import { UserInfoDownloadRequestsListComponent } from './user-info-download-requests-list/user-info-download-requests-list.component';
import { UserInfoDownloadRequestComponent } from './user-info-download-request/user-info-download-request.component';
import { UserInfoDownloadRequestsRoutingModule } from './user-info-download-requests-routing.module';

@NgModule({
    declarations: [
        UserInfoDownloadRequestsComponent,
        UserInfoDownloadRequestComponent,
        UserInfoDownloadRequestsListComponent,
    ],
    imports: [
        AtomsModule,
        ReactiveFormsModule,
        CommonModule,
        SharedModule,
        FormsModule,
        UserInfoDownloadRequestsRoutingModule
    ],
  })
  export class UserInfoDownloadRequestsModule { }
  