import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { NoticesListComponent } from './notices-list/notices-list.component';
import { CreateNoticeComponent } from './create-notice/create-notice.component';
import { NoticeComponent } from './notice/notice.component';

const routes: Routes = [
  {
    path: "",
    component: NoticesListComponent
  },
  {
    path: "create",
    component: CreateNoticeComponent
  },
  {
    path: ":id",
    component: NoticeComponent
  },
  
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class NoticesRoutingModule { }
