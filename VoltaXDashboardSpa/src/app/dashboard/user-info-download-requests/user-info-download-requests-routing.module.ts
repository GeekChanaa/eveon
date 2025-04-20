import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { UserInfoDownloadRequestsListComponent } from './user-info-download-requests-list/user-info-download-requests-list.component';
import { UserInfoDownloadRequestComponent } from './user-info-download-request/user-info-download-request.component';
const routes: Routes = [
  {
    path: "",
    component: UserInfoDownloadRequestsListComponent
  },
  {
    path: ":id",
    component: UserInfoDownloadRequestComponent
  },
  
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class UserInfoDownloadRequestsRoutingModule { }
