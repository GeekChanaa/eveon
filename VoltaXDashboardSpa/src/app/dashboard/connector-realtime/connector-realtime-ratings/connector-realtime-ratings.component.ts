import { Component, Input, OnInit } from '@angular/core';
import { RatingService } from 'src/_services/rating.service';

@Component({
  selector: 'app-connector-realtime-ratings',
  templateUrl: './connector-realtime-ratings.component.html',
  styleUrls: ['./connector-realtime-ratings.component.sass']
})
export class ConnectorRealtimeRatingsComponent implements OnInit {

  @Input() chargePointID : any = {}
  ratings : any[] = [];

  constructor(
    private _ratingService: RatingService
  ) { }

  ngOnInit() {
    this.getChargePointRatings();
  }

  getChargePointRatings(){
    this._ratingService.getChargePointRatings(this.chargePointID).subscribe((data) => {
      this.ratings = data;
    })
  }

}
