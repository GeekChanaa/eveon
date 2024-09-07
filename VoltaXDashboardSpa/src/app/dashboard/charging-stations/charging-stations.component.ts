import { Component, OnInit } from '@angular/core';
import { ChargingStation } from 'src/_models/charging-station';
import { Pagination } from 'src/_models/pagination';
import { ChargingStationService } from 'src/_services/charging-station.service';
import { Router } from '@angular/router';
import { CityService } from 'src/_services/city.service';
import { ParkingTypeEnum } from 'src/_models/_enums/parking-type';
import { ChargingStationStatusEnum } from 'src/_models/_enums/charging-station-status';
import { ChargingStationCategoryEnum } from 'src/_models/_enums/charging-station-category';

@Component({
  selector: 'app-charging-stations',
  templateUrl: './charging-stations.component.html',
  styleUrls: ['./charging-stations.component.sass']
})
export class ChargingStationsComponent implements OnInit {
  ngOnInit(): void {
  }
}
