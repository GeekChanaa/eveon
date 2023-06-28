import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Order } from 'src/_models/order';
import { OrderService } from 'src/_services/order.service';

@Component({
  selector: 'app-recharge-orders',
  templateUrl: './recharge-orders.component.html',
  styleUrls: ['./recharge-orders.component.css']
})
export class RechargeOrdersComponent implements OnInit {

  // Data
  data : any[] = [];

  // item params
  itemParams : any = {};

  // Fields
  fields : string[] = [];

  // Page params
  itemsPerPage : number = 20;
  currentPage : number = 1;

  order : Order = {
    id: 0,
    CardID: 0,
    Amount: 0,
    RechargeDate: new Date(),
    Card: null
  }

  // Constructor
  constructor(
    private _orderService : OrderService,
    private _router : Router
  ) { }

  ngOnInit() {
    this._getItemFields();
    this.getAll();
  }

  
  // Getting All Products
  getAll(){
    this._orderService.getAll(this.currentPage,this.itemsPerPage, this.itemParams).subscribe(data => {
      if (data.result) {
        this.data = data.result;
      }
    })
  }

  // Getting Item Fields
  private _getItemFields(){
    // Ensure this.chargingStation is defined
    if (!this.order || this.order == undefined) {
      return;
    }
    // Getting item fields
    Object.keys(this.order ?? {}).forEach((element : string) => {
      console.log(element); 
      if(typeof this.order?.[element] == "object" && this.order?.[element] != null && this.order?.[element].constructor.name == "Date")
      this.fields.push(element);
      if(typeof this.order?.[element] != "object") this.fields.push(element);
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

  delete(id : number){
    this._orderService.deleteById(id).subscribe((data) => {
      this.getAll();
    })
  }

  display(id : number){
    this._router.navigate(['/charging-cards/',id]);
  }

  update(id : number){
    console.log("updated");
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
