import { Injectable } from '@angular/core';
import { Comment } from 'src/_models/comment';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({
  providedIn: 'root'
})
export class CommentService extends AbstractService<Comment>{

  constructor(protected http : HttpClient, snackBar : MatSnackBar) {
    super(http, snackBar, environment.apiUrl+"/api/comment");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/comment";

}
