import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CommentsListComponent } from './comments-list/comments-list.component';
import { CreateCommentComponent } from './create-comment/create-comment.component';
import { CommentComponent } from './comment/comment.component';
const routes: Routes = [
  {
    path: "",
    component: CommentsListComponent
  },
  {
    path: "create",
    component: CreateCommentComponent
  },
  {
    path: ":id",
    component: CommentComponent
  },
  
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class CommentsRoutingModule { }
