import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { OcppReportingService } from 'src/_services/ocpp-services/ocpp-reporting.service';
import { UtilsService } from 'src/_services/utils.service';

@Component({
  selector: 'app-request-get-base-report',
  templateUrl: './request-get-base-report.component.html',
  styleUrls: ['./request-get-base-report.component.sass']
})
export class RequestGetBaseReportComponent implements OnInit {

  @Input() chargePointID : string = "";
  @Output() successEvent : EventEmitter<void> = new EventEmitter();
  reportBase : string = "SummaryInventory";
  
  constructor(
    private _OCPPreportService : OcppReportingService,
    private _modalService: ActionModalService,
    private _utils : UtilsService
  ) { }

  ngOnInit() {
  }

  getBaseReport(){
    let getBaseReportRequest = {
      requestId :  this._utils.generateRandomInteger(),
      reportBase : this.reportBase
    };
    console.log("this is the basereport request");
    console.log(getBaseReportRequest);
    this._OCPPreportService.getBaseReport(this.chargePointID, getBaseReportRequest).subscribe((data) => {
      this._modalService.popup(ActionModalStatusEnum.Success,"Success","Base Report Generated successfully",4000);
      this.successEvent.emit();
    },(error)=> {
      this._modalService.popup(ActionModalStatusEnum.Error,"Something Went Wrong","Something Went wrong please try again later", 4000);
    })
  }


}
