import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ListFilterDef } from 'src/_models/list-filter';
import { ReportCategoryEnum } from 'src/_models/_enums/report-category';
import { ReportCriticality } from 'src/_models/_enums/report-criticality';
import { ReportStatusEnum } from 'src/_models/_enums/report-status';
import { SystemReport } from 'src/_models/system-report';
import { SystemReportService } from 'src/_services/system-report.service';

@Component({
  selector: 'app-system-reports-list',
  templateUrl: './system-reports-list.component.html',
  styleUrls: ['./system-reports-list.component.sass']
})
export class SystemReportsListComponent implements OnInit {
  

  systemReport: SystemReport = {
    id: 0,
    reportCategory: ReportCategoryEnum.General,
    userID: 0,
    cardID: 0,
    status: ReportStatusEnum.Pending,
    connectorID: 0,
    chargePointID: 0,
    resolvedByID: 0,
    assignedID: 0,
    issueDescription: '',
    isEmail: false,
    isNotification: false,
    criticality: ReportCriticality.Informational
  };

  fields: string[] = [];
  filterDefs: ListFilterDef[] = [
    { key: 'status', label: 'Status', type: 'multiselect', options: [{ value: 'Pending', label: 'Pending' }, { value: 'InProgress', label: 'In progress' }, { value: 'Resolved', label: 'Resolved' }, { value: 'Closed', label: 'Closed' }] },
    { key: 'criticality', label: 'Criticality', type: 'multiselect', options: [{ value: 'Informational', label: 'Informational' }, { value: 'Low', label: 'Low' }, { value: 'Medium', label: 'Medium' }, { value: 'High', label: 'High' }, { value: 'Critical', label: 'Critical' }] },
    { key: 'reportCategory', label: 'Category', type: 'multiselect', options: [{ value: 'General', label: 'General' }, { value: 'Technical', label: 'Technical' }, { value: 'Maintenance', label: 'Maintenance' }] },
    { key: 'assigned', field: 'AssignedID', label: 'Assigned', type: 'boolean', presence: true },
    { key: 'assignee', field: 'Assigned-Email', label: 'Assignee email', type: 'text' },
    { key: 'chargePoint', field: 'ChargePoint-ChargePointId', label: 'Charge point', type: 'text' },
    { key: 'createdAt', label: 'Reported', type: 'date-range' },
  ];

  // Constructor
  constructor(
    private _systemReportService: SystemReportService,
    private _router : Router
  ) { }

  ngOnInit() {
    this._getItemFields();
  }

  getSystemReportsObservable = (currentPage:  number | undefined, itemsPerPage : number | undefined, itemParams : any) => this._systemReportService.getAll(currentPage, itemsPerPage, itemParams);
  deleteSystemReportObservable = (id : number) => this._systemReportService.deleteById(id);
  updateSystemReportObservable = (id : number, model : any) => this._systemReportService.edit(id, model);

  private _getItemFields() {
    if (!this.systemReport || this.systemReport == undefined) {
      return;
    }
    Object.keys(this.systemReport ?? {}).forEach((element: string) => {
      if (typeof this.systemReport?.[element] == "object" && this.systemReport?.[element] != null && this.systemReport?.[element].constructor.name == "Date")
        this.fields.push(element);
      if (typeof this.systemReport?.[element] != "object") this.fields.push(element);
    });
  }

}
