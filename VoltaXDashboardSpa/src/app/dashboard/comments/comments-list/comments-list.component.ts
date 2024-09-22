import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommentService } from 'src/_services/comment.service';
import { Comment } from 'src/_models/comment';
@Component({
  selector: 'app-comments-list',
  templateUrl: './comments-list.component.html',
  styleUrls: ['./comments-list.component.sass']
})
export class CommentsListComponent implements OnInit {

  fields: string[] = [];
  filters : any = {
    category:"",
    city : ""
  };
  

  comment: Comment = {
    id: 0,
    UserID: 0,
    Rating: 0,
    Text: '',
    ChargingStationID: 0,
    PointID: 0,
    CommentTime: new Date(),
  }

  // Constructor
  constructor(
    private _commentService: CommentService,
    private _router : Router
  ) { }

  ngOnInit() {
    this._getItemFields();
  }

  getCommentsObservable = (currentPage:  number | undefined, itemsPerPage : number | undefined, itemParams : any) => this._commentService.getAll(currentPage, itemsPerPage, itemParams);
  deleteCommentObservable = (id : number) => this._commentService.deleteById(id);
  updateCommentObservable = (id : number, model : any) => this._commentService.edit(id, model);

  private _getItemFields() {
    if (!this.comment || this.comment == undefined) {
      return;
    }
    Object.keys(this.comment ?? {}).forEach((element: string) => {
      if (typeof this.comment?.[element] == "object" && this.comment?.[element] != null && this.comment?.[element].constructor.name == "Date")
        this.fields.push(element);
      if (typeof this.comment?.[element] != "object") this.fields.push(element);
    });
  }

  resetFilters(){
    this.filters = {
      category:"",
      city : ""
    }
  }
}
