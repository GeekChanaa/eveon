import { Component, OnInit } from '@angular/core';
import { FormGroup, FormControl } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { EnumMappingService } from 'src/_services/enum-mapping.service';
import { NoticeService } from 'src/_services/notice.service';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-notice',
  templateUrl: './notice.component.html',
  styleUrls: ['./notice.component.sass']
})
export class NoticeComponent implements OnInit {


  noticeID : number = 0;
  noticeLoaded : boolean = false;
  parkingTypeValues : { [key: number]: string; } = {};
  noticeStatusValues : { [key: number]: string; } = {};
  noticeCategoryValues : { [key: number]: string; } = {};
  updateNoticeObservable = (id : number, model : any) => this._noticeService.edit(id, model);

  notice: any = {};

  staticUrl : string = environment.apiStaticFilesUrl;

  // Form group
  noticeForm : FormGroup;

  constructor(
    private _noticeService: NoticeService,
    private _route: ActivatedRoute,
    private _enumService : EnumMappingService
  ) {
    this.noticeForm = new FormGroup({
    })
   }

  ngOnInit() {
    var idParam = this._route.snapshot.paramMap.get('id')
    if (idParam != null) {
      var id = parseInt(idParam);
      this.getNoticeByID(id);
    }
    this.populatingSelectBoxes();
  }

  populatingSelectBoxes(){
    this._enumService.getEnumMapping("NoticeCategoryEnum");
  }

  getNoticeByID(id : number){
    this.noticeID = id;
    this._noticeService.getNoticeByID(id).subscribe((cs) => {
      this.notice = cs;
      this.noticeLoaded = true;
    })
  }

}
