import { Injectable } from '@angular/core';
import { ChargingStation } from 'src/_models/ChargingStationClass';

@Injectable({
  providedIn: 'root'
})
export class ChargingStationService {
  chargingStations: ChargingStation[] = [];

  constructor() {
    this.loadStations();
  }

  loadStations() {
    const stations = localStorage.getItem('stations');
    this.chargingStations = stations ? JSON.parse(stations) : [];
  }

  saveStations() {
    localStorage.setItem('stations', JSON.stringify(this.chargingStations));
  }

  getStations() {
    return this.chargingStations;
  }

  addStation(station: ChargingStation) {
    this.chargingStations.push(station);
    this.saveStations();
  }

  updateStation(index: number, station: ChargingStation) {
    this.chargingStations[index] = station;
    this.saveStations();
  }

  deleteStation(index: number) {
    this.chargingStations.splice(index, 1);
    this.saveStations();
  }

}
