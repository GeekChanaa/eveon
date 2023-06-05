import { Component, OnInit } from '@angular/core';
import { Customer } from 'src/_models/customer';
import { CustomerService } from 'src/_services/customer.service';

@Component({
  selector: 'app-customers',
  templateUrl: './customers.component.html',
  styleUrls: ['./customers.component.css']
})
export class CustomersComponent implements OnInit {

  // Data
  data : any[] = [];

  // Fields
  fields : string[] = [];

  // Page params
  itemsPerPage : number = 20;
  currentPage : number = 1;

  customer : Customer = {
    id: 0,
    UserID: 0,
    Sold: false,
    User: null
  }

  // Constructor
  constructor(
    private _customerService : CustomerService
  ) { }

  ngOnInit() {
    this._getItemFields();
    this.getAll();
  }

  
  // Getting All Products
  getAll(){
    this._customerService.getAll(this.currentPage,this.itemsPerPage).subscribe(data => {
      if (data.result) {
        this.data = data.result;
      }
    })
  }

  // Getting Item Fields
  private _getItemFields(){
    // Ensure this.chargingStation is defined
    if (!this.customer || this.customer == undefined) {
      return;
    }
    // Getting item fields
    Object.keys(this.customer ?? {}).forEach((element : string) => {
      console.log(element); 
      if(typeof this.customer?.[element] == "object" && this.customer?.[element] != null && this.customer?.[element].constructor.name == "Date")
      this.fields.push(element);
      if(typeof this.customer?.[element] != "object") this.fields.push(element);
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
