import { Component, OnInit } from '@angular/core';
import { NoticeListDto } from 'src/_models/_dtos/notice-dtos/notice-list-dto';
import { NoticeTypeEnum } from 'src/_models/_enums/notice-type-enum';
import { NoticeService } from 'src/_services/notice.service';

@Component({
  selector: 'app-notices-list',
  templateUrl: './notices-list.component.html',
  styleUrls: ['./notices-list.component.sass']
})
export class NoticesListComponent implements OnInit {
  fields: string[] = [];
  filters : any = {
    type:"",
  };
  

  notice: NoticeListDto = {
    title: '',
    type: NoticeTypeEnum.LegalUpdate,
    isEmail: false,
    isSms: false,
    isPushNotification: false,
    forAdmins: false,
    forSupports: false,
    forPartners: false,
    forUsers: false
  }

  // Constructor
  constructor(
    private _noticeService: NoticeService
  ) { }

  ngOnInit() {
    this._getItemFields();
  }

  getNoticesObservable = (currentPage:  number | undefined, itemsPerPage : number | undefined, itemParams : any) => this._noticeService.getAllNotices(currentPage, itemsPerPage, itemParams);
  deleteNoticeObservable = (id : number) => this._noticeService.deleteById(id);
  updateNoticeObservable = (id : number, model : any) => this._noticeService.edit(id, model);

  private _getItemFields() {
    if (!this.notice || this.notice == undefined) {
      return;
    }
    Object.keys(this.notice ?? {}).forEach((element: string) => {
      if (typeof this.notice?.[element] == "object" && this.notice?.[element] != null && this.notice?.[element].constructor.name == "Date")
        this.fields.push(element);
      if (typeof this.notice?.[element] != "object") this.fields.push(element);
    });
  }
  

  resetFilters(){
    this.filters = {
      type : ""
    }
  }
}
