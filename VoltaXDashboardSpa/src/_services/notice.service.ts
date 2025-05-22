import { Injectable } from '@angular/core';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';

import { Observable } from 'rxjs';
import { PaginatedResult } from 'src/_models/pagination';
import { Notice } from 'src/_models/notices/notice';
import { CreateNoticeDto } from 'src/_models/_dtos/notice-dtos/create-notice-dto';
import { NoticeListDto } from 'src/_models/_dtos/notice-dtos/notice-list-dto';
import { DisplayNoticeDto } from 'src/_models/_dtos/notice-dtos/display-notice-dto';

@Injectable({
  providedIn: 'root'
})
export class NoticeService extends AbstractService<Notice>{

  constructor(protected http : HttpClient) {
    super(http,environment.apiUrl+"/api/Notice/");
  }

  baseUrl = environment.apiUrl+"/api/Notice/";

  createNotice(notice : FormData){
    return this.http.post(this.baseUrl+"CreateNotice/",notice);
  }

  getAllNotices(page?: number, itemsPerPage?: number, itemParams?: any){
    return super.getAll(page,itemsPerPage,itemParams,"GetNotices");
  }

  getNoticeByID(noticeID : number){
    return this._http.get<DisplayNoticeDto>(this.baseUrl+"GetNoticeByID/"+noticeID)
  }


}
