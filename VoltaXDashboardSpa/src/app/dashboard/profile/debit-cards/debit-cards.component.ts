import { Component, Input, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { DebitCard } from 'src/_models/debit-card';
import { AuthService } from 'src/_services/auth.service';
import { DebitCardService } from 'src/_services/debit-card.service';
import { UserService } from 'src/_services/user.service';

@Component({
  selector: 'app-debit-cards',
  templateUrl: './debit-cards.component.html',
  styleUrls: ['./debit-cards.component.sass'],
})
export class DebitCardsComponent implements OnInit {
  showDCForm: Boolean = false;

  @Input() userID: number = 0;
  
  // Forms : 
  debitCardForm! : FormGroup; 

  debitCards : any[] = [];

  constructor(
    private _debitCardService : DebitCardService,
    private _userService : UserService,
    private _authService : AuthService
  ) {}

  ngOnInit() {
    this.debitCardForm = new FormGroup({
      debitCardName: new FormControl('', [Validators.required]),
      debitCardNumber: new FormControl('', [Validators.required, Validators.pattern(/^\d{16}$/)]),
      debitCardExpirationDate: new FormControl('', [Validators.required]),
      debitCardCVV: new FormControl('', [Validators.required, Validators.pattern(/^\d{3}$/)]),
    });

    // Getting User DEbit Cards
    this.getUserDebitCards();

  }

  // showAddDebitCardForm
  showAddDebitCardForm() {
    this.showDCForm = true;
  }

  // hideAddDebitCardForm
  hideAddDebitCardForm() {
    this.showDCForm = false;
  }

  // Save Debit Card
  debitCardSave() {
    var debitCardValue = this.debitCardForm.value;
    var debitCard: DebitCard = {
      id: 0,
      userID: this.userID,
      cardNumber: debitCardValue.debitCardNumber,
      name: debitCardValue.debitCardName,
      cvv: debitCardValue.debitCardCVV,
      expirationDate: this.convertToDate(
        debitCardValue.debitCardExpirationDate
      ),
    };
    this._debitCardService.create(debitCard).subscribe((data) => {
      this.getUserDebitCards();
    });
  }


  convertToDate(dateString: string): Date {
    // Split the string into month and year
    const parts = dateString.split('/');
    const month = parseInt(parts[0], 10);
    const year = parseInt(parts[1], 10);

    return new Date(year, month - 1, 1);
  }

  addSpaces(input: string): string {
    return input.replace(/(.{4})/g, '$1 ');
  }

  // Get User Debit cards
  getUserDebitCards(){
    var decodedToken = this._authService.getAuthInformation();
    var userid = parseInt(decodedToken.nameid);
    this._userService.getUserDebitCards(userid).subscribe((data) => {
      this.debitCards = data;
      this.showDCForm = false;
    })
  }

  // delete debit card
  deleteDebitCardByID(id : number){
    console.log("this is the debit card delete");
    this._debitCardService.deleteById(id).subscribe((data) => {
      this.getUserDebitCards();
    })
  }
}
