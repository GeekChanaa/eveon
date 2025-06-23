import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ReportService } from 'src/_services/report.service';
import { Report } from 'src/_models/report';
import { ReportTypeEnum } from 'src/_models/_enums/report-type';
import { ReportCategoryEnum } from 'src/_models/_enums/report-category';
import { ReportStatusEnum } from 'src/_models/_enums/report-status';
import { ReportListDto } from 'src/_models/_dtos/reports-dtos/report-list-dto';

@Component({
  selector: 'app-reports-list',
  templateUrl: './reports-list.component.html',
  styleUrls: ['./reports-list.component.sass']
})
export class ReportsListComponent implements OnInit {

  fields: string[] = [];
  filters : any = {
    reportCategory:"",
    reportType:"",
    status:"",
  };
  

  report: ReportListDto = {
    id: 0,
    userName: '',
    connectorName: '',
    chargePointName: '',
    reportType: ReportTypeEnum.ChargePoint,
    reportCategory: ReportCategoryEnum.General,
    issueDescription: '',
    status: ReportStatusEnum.Pending,
    reportDate: new Date(),
    resolvedDate: new Date()
  }

  // Constructor
  constructor(
    private _reportService: ReportService,
    private _router : Router
  ) { }

  ngOnInit() {
    this._getItemFields();
  }

  getReportsObservable = (currentPage:  number | undefined, itemsPerPage : number | undefined, itemParams : any) => this._reportService.getAllReports(currentPage, itemsPerPage, itemParams);
  deleteReportObservable = (id : number) => this._reportService.deleteById(id);
  updateReportObservable = (id : number, model : any) => this._reportService.edit(id, model);

  private _getItemFields() {
    if (!this.report || this.report == undefined) {
      return;
    }
    Object.keys(this.report ?? {}).forEach((element: string) => {
      if (typeof this.report?.[element] == "object" && this.report?.[element] != null && this.report?.[element].constructor.name == "Date")
        this.fields.push(element);
      if (typeof this.report?.[element] != "object") this.fields.push(element);
    });
  }

  resetFilters(){
    this.filters = {
      reportCategory:"",
      reportType:"",
      status : ""
    }
  }
}
