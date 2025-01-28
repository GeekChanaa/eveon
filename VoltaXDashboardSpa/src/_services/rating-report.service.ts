import { Injectable } from '@angular/core';
import { RatingReport } from 'src/_models/rating-report';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';


@Injectable({
  providedIn: 'root'
})
export class RatingReportService extends AbstractService<RatingReport>{

  constructor(protected http : HttpClient) {
    super(http,environment.apiUrl+"/api/ratingReport/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/ratingReport/";
}
