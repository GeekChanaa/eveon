import { Component, Input, OnInit } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { AuthService } from 'src/_services/auth.service';
import { RatingReportService } from 'src/_services/rating-report.service';
import { RatingService } from 'src/_services/rating.service';

@Component({
  selector: 'app-connector-realtime-ratings',
  templateUrl: './connector-realtime-ratings.component.html',
  styleUrls: ['./connector-realtime-ratings.component.sass']
})
export class ConnectorRealtimeRatingsComponent implements OnInit {

  @Input() chargePointID : any = {}
  ratings : any[] = [];
  currentPage : number = 1;
  pagination : any = {};
  isLoading : boolean = false;
  isReportModalVisible : boolean = false;
  reportedRating : any = {};
  report : any = {};
  userID : number = 0;
  isReportLoading : boolean = false;

  constructor(
    private _ratingService: RatingService,
    private _modalService : ActionModalService,
    private _authService: AuthService,
    private _ratingReportService : RatingReportService,
  ) { }

  ngOnInit() {
    this.userID = parseInt(this._authService.getAuthInformation().nameid);
    this.getChargePointRatings();
  }

  getChargePointRatings(page : number = 1){
    this.isLoading = true;
      this._ratingService.getChargePointRatings(this.chargePointID, page,5).subscribe((data) => {
      this.isLoading = false;
      if(data.result)
        this.ratings = data.result;
      if(data.pagination)
        this.pagination = data.pagination;
    },(error) => {
      this._modalService.popup(ActionModalStatusEnum.Error, "Error", " Something went wrong please try again later",4000);
    })
  }

  openReportModal(rating : any ){
    this.isReportModalVisible = true;
    this.report.ratingID = rating.id;
    this.report.userID = this.userID;
    this.reportedRating = rating;
  }

  closeModal(){
    this.isReportModalVisible = false;
    this.reportedRating = {};
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
