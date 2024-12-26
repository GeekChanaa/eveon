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
  requestId : number = 0;
  isHovered : boolean = false;
  

  isLoading : boolean = false;
  isRequested : boolean = false;
  showCheckIcon : boolean = false;

  constructor(
    private _OCPPreportService : OcppReportingService,
    private _modalService: ActionModalService,
    private _utils : UtilsService
  ) { }

  ngOnInit() {
  }

  getBaseReport(){
    this.isLoading = true;
    let getBaseReportRequest = {
      requestId :  this._utils.generateRandomInteger(),
      reportBase : this.reportBase
    };
    this._OCPPreportService.getBaseReport(this.chargePointID, getBaseReportRequest).subscribe((data) => {
      this.isLoading = true;
      this.requestId = data.requestID;
      this.isRequested = true;
      this._modalService.popup(ActionModalStatusEnum.Success,"Success","Base Report Generated successfully",4000);
      this.successEvent.emit();
    },(error)=> {
      this.isLoading = true;
      this._modalService.popup(ActionModalStatusEnum.Error,"Something Went Wrong","Something Went wrong please try again later", 4000);
    })
  }

  copyToClipboard(): void {
    navigator.clipboard.writeText(this.requestId.toString()).then(() => {
      this.showCheckIcon = true;
      setTimeout(() => {
        this.showCheckIcon = false;
      }, 5000); // 5 seconds
    }).catch(err => {
      console.error('Failed to copy: ', err);
    });
  }


}
