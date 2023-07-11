import { Component, OnInit } from '@angular/core';
import { Connector } from 'src/_models/connector';
import { ConnectorService } from 'src/_services/connector.service';

@Component({
  selector: 'app-connectors',
  templateUrl: './connectors.component.html',
  styleUrls: ['./connectors.component.css']
})
export class ConnectorsComponent implements OnInit {

  // Data
  data : any[] = [];

  // Fields
  fields : string[] = [];

  // Page params
  itemsPerPage : number = 20;
  currentPage : number = 1;

  // item params
  itemParams : any = {};

  connector : Connector = {
    id: 0,
    chargePointID: 0,
    power: 0,
    speed: 0
  }

  // Constructor
  constructor(
    private _connectorService : ConnectorService
  ) { }

  ngOnInit() {
    this._getItemFields();
    this.getAll();
  }

  
  // Getting All Products
  getAll(){
    this._connectorService.getAll(this.currentPage,this.itemsPerPage, this.itemParams).subscribe(data => {
      if (data.result) {
        this.data = data.result;
        console.log(this.data);
      }
    })
  }

  // Getting Item Fields
  private _getItemFields(){
    // Ensure this.connector is defined
    if (!this.connector || this.connector == undefined) {
      return;
    }
    // Getting item fields
    Object.keys(this.connector ?? {}).forEach((element : string) => {
      console.log(element); 
      if(typeof this.connector?.[element] == "object" && this.connector?.[element] != null && this.connector?.[element].constructor.name == "Date")
      this.fields.push(element);
      if(typeof this.connector?.[element] != "object") this.fields.push(element);
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

}
