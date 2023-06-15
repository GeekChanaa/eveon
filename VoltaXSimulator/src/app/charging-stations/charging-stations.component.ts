import { Component, OnInit } from '@angular/core';
import { ChargingStation } from 'src/_models/ChargingStationClass';
import { ChargingStationService } from 'src/_services/charging-station.service';

@Component({
  selector: 'app-charging-stations',
  templateUrl: './charging-stations.component.html',
  styleUrls: ['./charging-stations.component.css']
})
export class ChargingStationsComponent implements OnInit {

  name = '';
  address = '';
  chargingStationID = '';
  editIndex = -1;

  constructor(public stationService: ChargingStationService) {}

  ngOnInit(): void {}

  addOrUpdateStation() {
    const station = new ChargingStation(this.name, this.address, this.chargingStationID);
    if (this.editIndex === -1) {
      this.stationService.addStation(station);
    } else {
      this.stationService.updateStation(this.editIndex, station);
      this.editIndex = -1;
    }
    this.clearForm();
  }

  deleteStation(index: number) {
    this.stationService.deleteStation(index);
  }

  editStation(index: number) {
    const station = this.stationService.getStations()[index];
    this.name = station.name;
    this.address = station.address;
    this.chargingStationID = station.chargingStationID;
    this.editIndex = index;
  }

  clearForm() {
    this.name = '';
    this.address = '';
    this.chargingStationID = '';
  }

}
