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
  styleUrls: ['./charging-stations.component.css']
})
export class ChargingStationsComponent implements OnInit {

  // Data
  data: any[] = [];

  // cities : 
  cities : any[] = [];

  // filters : 
  filters : any = {};

  // Fields
  fields: string[] = [];

  // params for filtering / sotring
  itemParams : any = {};

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
    category: ChargingStationCategoryEnum.Public,
    chargerQuantity: '',
    country: '',
    state: '',
    city: '',
    latitude: '',
    longitude: '',
    organisation: '',
    parkingType: ParkingTypeEnum.AngleParking,
    status: ChargingStationStatusEnum.Available,
    wifiAmenity: '',
    parkingAmenity: '',
    restaurantsAmenity: '',
    washroomAmenity: '',
    sittingAreaAmenity: '',
    chargePoints: []
  }

  // Constructor
  constructor(
    private _chargingStationService: ChargingStationService,
    private _router : Router,
    private _cityService : CityService
  ) { }

  ngOnInit() {
    this.getAllMoroccoCityNames();
    this._getItemFields();
    this.getAll();
  }


  // Getting All Products
  getAll() {
    this._chargingStationService.getAll(this.currentPage, this.itemsPerPage, this.itemParams).subscribe(data => {
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

  // delete item
  delete(id : number){
    this._chargingStationService.deleteById(id).subscribe((data) => {
      this.getAll();
    })
  }

  // delete item
  update(id : number){
    console.log("function to be implemented");
  }

  // delete item
  display(id : number){
    this._router.navigate(['/dashboard/charging-stations',id])
  }

  // sorting by field
  sort(field : string){
    if(this.itemParams.orderBy == field){
      if(this.itemParams.reverseOrder == 'y')
      this.itemParams.reverseOrder = 'n'
      else
      this.itemParams.reverseOrder = 'y'
    }
    else{
      this.itemParams.orderBy = field;
      this.itemParams.reverseOrder = 'n'
    }
    this.getAll();
  }

  search(val : string){
     // Update parameters in itemParams
    this.itemParams.SearchBy = ['Name', 'Address']; // array of fields to search in
    this.itemParams.SearchValue = 'Gut'; // the value to search for
    this.getAll();
  }

  // Getting Morocco Cities: 
  getAllMoroccoCityNames(){
    this._cityService.getAllMoroccoCityNames().subscribe((data) => {
      this.cities = data;
    })
  }

  // Applying filters
  applyFilters(){
    this.itemParams.FilterValue = [ this.filters.city, this.filters.category]; 
    this.itemParams.FilterBy = ["City","Category"]; 
    this.getAll();
  }

}
