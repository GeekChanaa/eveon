import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommentService } from 'src/_services/comment.service';
import { EnumMappingService } from 'src/_services/enum-mapping.service';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-comment',
  templateUrl: './comment.component.html',
  styleUrls: ['./comment.component.sass']
})
export class CommentComponent implements OnInit {

  commentID : number = 0;
  commentLoaded : boolean = false;
  CardTypesValues : any = {};
  CardStatusesValues : any = {};
  updateCommentObservable = (id : number, model : any) => this._commentService.edit(id, model);

  comment: any = {};

  staticUrl : string = environment.apiStaticFilesUrl;

  constructor(
    private _commentService: CommentService,
    private _route: ActivatedRoute,
    private _enumService : EnumMappingService
  ) {
   }

  ngOnInit() {
    var idParam = this._route.snapshot.paramMap.get('id')
    if (idParam != null) {
      var id = parseInt(idParam);
      this.getCommentByID(id);
    }
  }


  getCommentByID(id : number){
    this.commentID = id;
    this._commentService.getCommentByID(id).subscribe((cs) => {
      this.comment = cs;
      this.commentLoaded = true;
    })
  }

}
