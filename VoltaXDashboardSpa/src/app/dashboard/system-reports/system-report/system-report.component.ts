import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Route, Router } from '@angular/router';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { PageState } from 'src/_models/_enums/page-state.enum';
import { SystemReportComment } from 'src/_models/system-report-comment';
import { ActionModalService } from 'src/_services/action-modal.service';
import { AuthService } from 'src/_services/auth.service';
import { SystemReportCommentService } from 'src/_services/system-report-comment.service';
import { SystemReportService } from 'src/_services/system-report.service';

enum SystemReportTabsEnum{
  InformationsTab = 'InformationsTab',
  CommentsTab = 'CommentsTab'
}

@Component({
  selector: 'app-system-report',
  templateUrl: './system-report.component.html',
  styleUrls: ['./system-report.component.sass']
})
export class SystemReportComponent implements OnInit {

  PageState = PageState;
  state: PageState = PageState.Loading;

  SystemReportTabsEnum = SystemReportCommentService;
  tabsEnum : SystemReportTabsEnum = SystemReportTabsEnum.InformationsTab;
  systemReport : any = {};

  systemReportLoaded = false;

  constructor(
    private _systemReportService:  SystemReportService,
    private _route : ActivatedRoute,
    private _systemReportCommentService:  SystemReportCommentService,
    private _modalService : ActionModalService,
  ) { }

  ngOnInit() {
    var idParam = this._route.snapshot.paramMap.get('id')
    if (idParam != null) {
      var id = parseInt(idParam);
      this.getSystemReport(id);
    }
  }

  getSystemReport(id : number){
    this.state = PageState.Loading;
    this._systemReportService.getSystemReportInformations(id).subscribe((data) => {
      this.state = PageState.Success;
      this.systemReportLoaded = true;
      this.systemReport = data;
      console.log(this.systemReport)
    })
  }

  deleteSystemReport(id : number){
    this._systemReportCommentService.deleteById(id).subscribe((data) => {
      this._modalService.popup(ActionModalStatusEnum.Success,"Delted","System Report "+id+" deleted sucessfully",4000);
    }, (error) => {
      this._modalService.popup(ActionModalStatusEnum.Error,"Erro","Something Went wrong please contact your system administrator",4000);
    })
  }

  changeTab(tab : any){
    this.tabsEnum = tab;
  }

}
