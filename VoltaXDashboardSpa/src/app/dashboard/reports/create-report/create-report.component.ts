import { Component, OnInit } from '@angular/core';
import { FormGroup, FormControl, FormArray } from '@angular/forms';
import { Router } from '@angular/router';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { ChargePointService } from 'src/_services/charge-point.service';
import { ConnectorService } from 'src/_services/connector.service';
import { EnumMappingService } from 'src/_services/enum-mapping.service';
import { ReportService } from 'src/_services/report.service';
import { UserService } from 'src/_services/user.service';

@Component({
  selector: 'app-create-report',
  templateUrl: './create-report.component.html',
  styleUrls: ['./create-report.component.sass']
})
export class CreateReportComponent implements OnInit {

  form : FormGroup;
  report : any = {};
  users : any[] = [];
  connectors : any[] = [];
  chargePoints : any[] = [];

  ngAfterViewInit() {
  }

  ngOnInit() {
    this.getAllUserNames();
    this.getAllConnectorNames();
    this.getAllChargePointNames();
  }

  opacity: number = 0;
  activeDiv = 1;


  reportTypes : any = {};
  reportStatuses : any = {};



  constructor(
    private _enumService : EnumMappingService,
    private _modalService:  ActionModalService,
    private _router : Router,
    private _reportService : ReportService,
    private _userService: UserService,
    private _chargePointService: ChargePointService,
    private _connectorService : ConnectorService
  ) {
    this.form = new FormGroup({
      userID : new FormControl(''),
      connectorID : new FormControl(''),
      chargePointID : new FormControl(''),
      reportType : new FormControl('Website'),
      reportCategory : new FormControl('General'),
      issueDescription : new FormControl(''),
      status : new FormControl('Pending'),
    })

  }

  showSelect() {
    this.opacity = 1;
  }

  hideSelect() {
    this.opacity = 0;
  }

  getFormControl(name: string): FormControl {
    return this.form.get(name) as FormControl;
  }

  
  onSubmit(){
    var reportForm = this.form.value;
    this.report.userID = reportForm.userID;
    this.report.connectorID = reportForm.connectorID;
    this.report.chargePointID = reportForm.chargePointID;
    this.report.reportType = reportForm.reportType;
    this.report.reportCategory = reportForm.reportCategory;
    this.report.issueDescription = reportForm.issueDescription;
    this.report.status = reportForm.status;

    console.log("this is the report i'm truing to create");

    this._reportService.createReport(this.report).subscribe((createdReport) => {
      this._modalService.popup(ActionModalStatusEnum.Success,"Succcess !","Report Created Successfully",4000);
      this._router.navigateByUrl('/dashboard/reports');
    },(error) => {
      this._modalService.popup(ActionModalStatusEnum.Error,"Error","Something went wrong",4000);
    });
  }

  

  getAllUserNames(){
    this._userService.getUserNames().subscribe((data) => {
      this.users = data;
    })
  }

  updateUser(user : any){
    this.userControl.setValue(user.id);
  }

  get userControl(): FormControl {
    const control = this.form.get('userID');
    if (!control) {
      throw new Error('User control not found');
    }
    return control as FormControl;
  }



  getAllChargePointNames(){
    this._chargePointService.getChargePointIds().subscribe((data) => {
      this.chargePoints = data;
    })
  }

  updateChargePoint(chargePoint : any){
    this.chargePointControl.setValue(chargePoint.id);
  }

  get chargePointControl(): FormControl {
    const control = this.form.get('chargePointID');
    if (!control) {
      throw new Error('ChargePoint control not found');
    }
    return control as FormControl;
  }

  getAllConnectorNames(){
    this._connectorService.getConnectorIds().subscribe((data) => {
      console.log("this is getting the connectors");
      console.log(data);
      this.connectors = data;
    })
  }

  updateConnector(connector : any){
    this.connectorControl.setValue(connector.id);
  }

  get connectorControl(): FormControl {
    const control = this.form.get('connectorID');
    if (!control) {
      throw new Error('Connector control not found');
    }
    return control as FormControl;
  }

}
