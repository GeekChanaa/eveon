import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { AtomsModule } from 'src/app/atoms/atoms.module';
import { SharedModule } from 'src/app/shared/shared.module';
import { CommentsRoutingModule } from './comments-routing.module';
import { CommentsComponent } from './comments.component';
import { CommentsListComponent } from './comments-list/comments-list.component';
import { CreateCommentComponent } from './create-comment/create-comment.component';
import { CommentComponent } from './comment/comment.component';


@NgModule({
  declarations: [
    CommentsComponent,
    CommentsListComponent,
    CreateCommentComponent,
    CommentComponent
  ],
  imports: [
      AtomsModule,
      ReactiveFormsModule,
      CommonModule,
      SharedModule,
      FormsModule,
      CommentsRoutingModule
  ],
})
export class CommentsModule { }
  