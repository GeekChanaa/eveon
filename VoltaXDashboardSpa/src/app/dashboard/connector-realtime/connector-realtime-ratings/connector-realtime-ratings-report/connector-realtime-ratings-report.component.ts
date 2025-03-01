import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { RatingReportService } from 'src/_services/rating-report.service';

@Component({
  selector: 'app-connector-realtime-ratings-report',
  templateUrl: './connector-realtime-ratings-report.component.html',
  styleUrls: ['./connector-realtime-ratings-report.component.sass']
})
export class ConnectorRealtimeRatingsReportComponent implements OnInit {

  @Input() report : any = {};
  @Input() reportedRating : any = {};
  @Output() closeModalEvent : EventEmitter<void> = new EventEmitter();
  isReportLoading : boolean = false;


  constructor(
    private _ratingReportService : RatingReportService,
    private _modalService : ActionModalService
  ) { }

  ngOnInit() {
  }

  closeModal(){
    this.closeModalEvent.emit();
  }

  reportRating(){
    this.isReportLoading = true;
    this._ratingReportService.create(this.report).subscribe((data) => {
      this.isReportLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Success,"Success","Thanks you for your report, we will take this seriously",4000);
      this.closeModal();
    },(error)=> {
      this.isReportLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Error,"Something Went Wrong","Something Went wrong please try again later", 4000);
    })
  }

}
