import { Component, OnInit } from '@angular/core';
import { ChargingStation } from 'src/_models/charging-station';
import { Pagination } from 'src/_models/pagination';
import { ChargingStationService } from 'src/_services/charging-station.service';

@Component({
  selector: 'app-charging-stations',
  templateUrl: './charging-stations.component.html',
  styleUrls: ['./charging-stations.component.css']
})
export class ChargingStationsComponent implements OnInit {

  // Data
  data: any[] = [];

  // Fields
  fields: string[] = [];

  // Page params
  itemsPerPage: number = 20;
  currentPage: number = 1;

  // Pagination Data
  pagination: Pagination = {
    currentPage: 0,
    itemsPerPage: 0,
    totalItems: 0,
    totalPages: 0
  }

  chargingStation: ChargingStation = {
    id: 0,
    name: '',
    address: '',
    network: '',
    category: '',
    chargerQuantity: '',
    country: '',
    state: '',
    city: '',
    latitude: '',
    longitude: '',
    organisation: '',
    parkingType: '',
    status: '',
    wifiAmenity: '',
    parkingAmenity: '',
    restaurantsAmenity: '',
    washroomAmenity: '',
    sittingAreaAmenity: '',
    ChargePoints: []
  }

  // Constructor
  constructor(
    private _chargingStationService: ChargingStationService
  ) { }

  ngOnInit() {
    this._getItemFields();
    this.getAll();
  }


  // Getting All Products
  getAll() {
    this._chargingStationService.getAll(this.currentPage, this.itemsPerPage).subscribe(data => {
      if (data.result) {
        this.data = data.result;

      }
      if (data.pagination) {
        this.pagination = data.pagination;
      }
    })
  }

  // Getting Item Fields
  private _getItemFields() {
    // Ensure this.chargingStation is defined
    if (!this.chargingStation || this.chargingStation == undefined) {
      return;
    }
    // Getting item fields
    Object.keys(this.chargingStation ?? {}).forEach((element: string) => {
      console.log(element);
      if (typeof this.chargingStation?.[element] == "object" && this.chargingStation?.[element] != null && this.chargingStation?.[element].constructor.name == "Date")
        this.fields.push(element);
      if (typeof this.chargingStation?.[element] != "object") this.fields.push(element);
    });
  }

  // Deleting the item

  // Next page
  nextPage() {
    this.currentPage++;
    this.getAll();
  }

  // Previous Page
  previousPage() {
    this.currentPage--;
    this.getAll();
  }

  // First Page
  goToFirstPage() {
    this.currentPage = 1
    this.getAll()
  }

  // Last PAge
  goToLastPage() {
    this.currentPage = this.pagination.totalPages;
    this.getAll();
  }
}
