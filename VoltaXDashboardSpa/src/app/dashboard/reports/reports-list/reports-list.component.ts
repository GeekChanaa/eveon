import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ListFilterDef } from 'src/_models/list-filter';
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
  filterDefs: ListFilterDef[] = [
    { key: 'status', label: 'Status', type: 'multiselect', options: [{ value: 'Pending', label: 'Pending' }, { value: 'InProgress', label: 'In progress' }, { value: 'Resolved', label: 'Resolved' }, { value: 'Closed', label: 'Closed' }] },
    { key: 'reportCategory', label: 'Category', type: 'multiselect', options: [{ value: 'General', label: 'General' }, { value: 'Technical', label: 'Technical' }, { value: 'Maintenance', label: 'Maintenance' }] },
    { key: 'reportType', label: 'Type', type: 'multiselect', options: [{ value: 'ChargePoint', label: 'Charge point' }, { value: 'Connector', label: 'Connector' }, { value: 'Website', label: 'Website' }, { value: 'Other', label: 'Other' }] },
    { key: 'chargePoint', field: 'ChargePoint-ChargePointId', label: 'Charge point', type: 'text' },
    { key: 'reporter', field: 'User-Email', label: 'Reporter email', type: 'text' },
    { key: 'reportDate', label: 'Reported', type: 'date-range' },
    { key: 'resolvedDate', label: 'Resolved', type: 'date-range' },
  ];
  

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
}
