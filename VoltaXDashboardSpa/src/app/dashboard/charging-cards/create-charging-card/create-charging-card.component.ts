import { AfterViewInit, Component, ElementRef, OnInit, Renderer2 } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { CardStatusEnum } from 'src/_models/_enums/card-status';
import { Card } from 'src/_models/card';
import { CardService } from 'src/_services/card.service';
import { UserService } from 'src/_services/user.service';
declare var $: any;  

@Component({
  selector: 'app-create-charging-card',
  templateUrl: './create-charging-card.component.html',
  styleUrls: ['./create-charging-card.component.css']
})
export class CreateChargingCardComponent implements OnInit, AfterViewInit {

  
  // FormGroup
  form : FormGroup;

  // card
  card : Card = {
    id: 0,
    cardNumber: '',
    cardType: '',
    expirationDate: new Date(),
    maxCount: 0,
    status: CardStatusEnum.Inactive,
    balance: 0,
    note: '',
    userID: 0,
    user: null,
    transactions : [],
    orders : []
  };

  // Users
  users : any[] = [];


  ngAfterViewInit() {
    
  }
  

  constructor(
    private _chargingCardService:  CardService,
    private _userService : UserService,
    private renderer: Renderer2,
    private el: ElementRef
  ) { 
    this.form = new FormGroup({
      cardType : new FormControl(''),
      expirationDate : new FormControl(''),
      maxCount : new FormControl(''),
      status : new FormControl(''),
      balance : new FormControl(''),
      note : new FormControl(''),
      userID : new FormControl('')
    })
  }

  // Getting State Control
  get userControl(): FormControl {
    const control = this.form.get('userID');
    if (!control) {
      throw new Error('User control not found');
    }
    return control as FormControl;
  }

  ngOnInit() {
    this.getAllUserNames();
  }

  onSubmit(){
    var cardForm = this.form.value;
    this.card.cardType = cardForm.cardType;
    this.card.expirationDate = cardForm.expirationDate;
    this.card.maxCount = cardForm.maxCount;
    this.card.status = cardForm.status;
    this.card.note = cardForm.note;
    this.card.userID = cardForm.userID;
    this.card.balance = cardForm.balance;
    console.log("this is the card : ");
    console.log(this.card);
  }

  // Getting All users
  getAllUserNames(){
    this._userService.getUserNames().subscribe((data) => {
      this.users = data;
    })
  }

  // Getting all user names by name
  getAllUsersNamesByName(name : string){
    this._userService.getAllUsersNamesByName(name).subscribe((data) => {
      this.users = data;
    })
  }

  // User Selected Event
  updateUser(user : any){
    this.userControl.setValue(user.id);
  }

  // card number generator
  generateCardNumber(): string {
    let cardNumber = '';
    for(let i = 0; i < 16; i++) {
        cardNumber += Math.floor(Math.random() * 10);
    }
    return cardNumber;
  }

}
