import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Comment } from 'src/_models/comment';
import { environment } from 'src/environments/environment';
import { AbstractService } from './abstract-service';


@Injectable({
  providedIn: 'root'
})
export class CommentService extends AbstractService<Comment>{

  constructor(protected http : HttpClient) {
    super(http,environment.apiUrl+"/api/Comment/");
  }

  baseUrl = environment.apiUrl+"/api/Comment/";

  createComment(comment : any){
    return this._http.post<any>(this.baseUrl+"CreateComment",comment);
  }

  getCommentByID(commentID : number){
    return this._http.get<any>(this.baseUrl+"getCommentByID/"+commentID);
  }

}
