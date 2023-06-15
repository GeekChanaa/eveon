import { Component, OnInit } from '@angular/core';
import { ChargePoint } from 'src/_models/charge-point';
import { ChargePointService } from 'src/_services/charge-point.service';

@Component({
  selector: 'app-charging-points',
  templateUrl: './charging-points.component.html',
  styleUrls: ['./charging-points.component.css']
})
export class ChargingPointsComponent implements OnInit {

  // Data
  data : any[] = [];

  // Fields
  fields : string[] = [];

  // Page params
  itemsPerPage : number = 20;
  currentPage : number = 1;

  chargePoint : ChargePoint = {
    id: 0,
    chargePointId: '',
    chargingStationID: 0,
    name: '',
    serialNumber: '',
    make: '',
    status: '',
    comment: '',
    username: '',
    password: '',
    clientCertThumb: '',
    connectors: [],
    transactions: []
  }

  // Constructor
  constructor(
    private _chargePointService : ChargePointService
  ) { }

  ngOnInit() {
    this._getItemFields();
    this.getAll();
  }

  
  // Getting All Products
  getAll(){
    this._chargePointService.getAll(this.currentPage,this.itemsPerPage).subscribe(data => {
      if (data.result) {
        this.data = data.result;
      }
    })
  }

  // Getting Item Fields
  private _getItemFields(){
    // Ensure this.chargePoint is defined
    if (!this.chargePoint || this.chargePoint == undefined) {
      return;
    }
    // Getting item fields
    Object.keys(this.chargePoint ?? {}).forEach((element : string) => {
      console.log(element); 
      if(typeof this.chargePoint?.[element] == "object" && this.chargePoint?.[element] != null && this.chargePoint?.[element].constructor.name == "Date")
      this.fields.push(element);
      if(typeof this.chargePoint?.[element] != "object") this.fields.push(element);
    });
  }

  // Deleting the item

  // Next page
  nextPage(){
    this.currentPage++;
    this.getAll();
  }

  // Previous Page
  previousPage(){
    this.currentPage--;
    this.getAll();
  }

}
