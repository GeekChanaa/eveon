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
    this.getAllChargePointNames();
  }

  opacity: number = 0;
  activeDiv = 1;


  reportTypes : any = {};
  reportStatuses : any = {};

  isLoading : boolean = false;


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
      isEmail : new FormControl(false),
      isNotification : new FormControl(false),
    });
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
    this.isLoading = true;
    this.report = this.form.value;
    this._reportService.createReport(this.report).subscribe((createdReport) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Success,"Succcess !","Report Created Successfully",4000);
      this._router.navigateByUrl('/dashboard/reports');
    },(error) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Error,"Error","Something went wrong",4000);
    });
  }

  

  getAllUserNames(){
    this._userService.getUserNames().subscribe((data) => {
      this.users = data;
    })
  }

  getAllChargePointNames(){
    this._chargePointService.getChargePointIds().subscribe((data) => {
      this.chargePoints = data;
    })
  }

  getChargePointConnectors(chargePointID : number){
    this._connectorService.getChargePointConnectors(chargePointID).subscribe((data) => {
      this.connectors = data.map(connector => ({
        id: connector.id,
        name: `evseID: ${connector.evseID} - connectorID: ${connector.connectorID}`
      }));
    })
  }

  getControl(name: string): FormControl {
    return this.form.get(name) as FormControl;
  }

}
