import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { CardService } from 'src/_services/card.service';
import { CustomerService } from 'src/_services/customer.service';

@Component({
  selector: 'app-create-charging-card',
  templateUrl: './create-charging-card.component.html',
  styleUrls: ['./create-charging-card.component.css']
})
export class CreateChargingCardComponent implements OnInit {

  // FormGroup
  form : FormGroup;

  // Customers
  customers : any[] = [];

  constructor(
    private _chargingCardService:  CardService,
    private _customerService : CustomerService
  ) { 
    this.form = new FormGroup({
      cardNumber : new FormControl(''),
      account : new FormControl(''),
      cardType : new FormControl(''),
      expirationDate : new FormControl(''),
      maxCount : new FormControl(''),
      status : new FormControl(''),
      balance : new FormControl(''),
      note : new FormControl(''),
      customerID : new FormControl('')
    })
  }

  // Getting State Control
  get customerControl(): FormControl {
    const control = this.form.get('customerID');
    if (!control) {
      throw new Error('Country control not found');
    }
    return control as FormControl;
  }

  ngOnInit() {
    this.getAllCustomersNames();
  }

  onSubmit(){
    console.log(this.form.value);
  }

  // Getting All customers
  getAllCustomersNames(){
    this._customerService.getCustomersNames().subscribe((data) => {
      this.customers = data;
    })
  }

  // Getting all customer names by name
  getAllCustomersNamesByName(name : string){
    this._customerService.getCustomersNamesByName(name).subscribe((data) => {
      this.customers = data;
    })
  }

  // Customer Selected Event
  updateCustomer(customer : any){
    this.customerControl.setValue(customer.id);
  }
}
