import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { Router } from '@angular/router';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ReportCategoryEnum } from 'src/_models/_enums/report-category';
import { ReportCriticality } from 'src/_models/_enums/report-criticality';
import { ReportStatusEnum } from 'src/_models/_enums/report-status';
import { ActionModalService } from 'src/_services/action-modal.service';
import { CardService } from 'src/_services/card.service';
import { ChargePointService } from 'src/_services/charge-point.service';
import { ConnectorService } from 'src/_services/connector.service';
import { SystemReportService } from 'src/_services/system-report.service';
import { UserService } from 'src/_services/user.service';

@Component({
  selector: 'app-create-system-report',
  templateUrl: './create-system-report.component.html',
  styleUrls: ['./create-system-report.component.sass']
})
export class CreateSystemReportComponent implements OnInit {

  cards : any[] = [];
  supports : any[]=[];
  chargePoints: any[] =[];
  connectors : any[] =[];
  users : any[] = [];
  form: FormGroup;
  isLoading : boolean = false;
  reportCriticality = ReportCriticality;
  reportCategory = ReportCategoryEnum;
  reportStatus = ReportStatusEnum;


  constructor(
    private _connectorService: ConnectorService,
    private _userService: UserService,
    private _chargePointService: ChargePointService,
    private _cardService : CardService,
    private _modalService: ActionModalService,
    private _systemReportService : SystemReportService,
    private _router : Router
  ) { 
    this.form = new FormGroup({
      reportCategory : new FormControl(ReportCategoryEnum.General),
      userID : new FormControl(null),
      connectorID : new FormControl(null),
      cardID : new FormControl(null),
      chargePointID : new FormControl(null),
      assignedID : new FormControl(null),
      issueDescription : new FormControl(''), 
      isEmail : new FormControl(false),
      isNotification : new FormControl(false),
      status : new FormControl(ReportStatusEnum.Pending),
      criticality : new FormControl(ReportCriticality.Medium),
    });
  }

  getFormControl(name: string): FormControl {
    return this.form.get(name) as FormControl;
  }

  ngOnInit() {
    this.getUsers();
    this.getConnectors();
    this.getChargePoints();
    this.getUserSupports();
    this.getCards();
  }

  getCards(){
    this._cardService.getAllCards(1,-1).subscribe((data) => {
      if(data.result != null)
        this.cards = data.result;
    })
  }

  getUserSupports(){
    this._userService.getSupportUserNames().subscribe((data )=> {
      this.supports = data;
    })
  }

  getUsers(){
    this._userService.getUserNames().subscribe((data) => {
      this.users = data;
    })
  }

  getConnectors(){
    this._connectorService.getConnectorIds().subscribe((data) => {
      this.connectors = data;
    })
  }

  getChargePoints(){
    this._chargePointService.getChargePointIds().subscribe((data) => {
      this.chargePoints = data;
    })
  }

  onSubmit(){
    this.isLoading = true;
    let systemReport = (this.form.value);
    this._systemReportService.createSystemReport(systemReport).subscribe((data) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Success,"Succcess !","System Report Created.",4000);
      this._router.navigateByUrl("/dashboard/system-reports");
    },(error) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Error,"Error","Something went wrong",4000);
    })
  }



}
