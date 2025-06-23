import { Component, Input, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { Action } from 'rxjs/internal/scheduler/Action';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { DebitCard } from 'src/_models/debit-card';
import { ActionModalService } from 'src/_services/action-modal.service';
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
  

  debitCards : any[] = [];

  constructor(
    private _debitCardService : DebitCardService,
    private _userService : UserService,
    private _authService : AuthService,
    private _modalService : ActionModalService
  ) {}

  ngOnInit() {
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
    this._debitCardService.deleteById(id).subscribe((data) => {
      this.getUserDebitCards();
      this._modalService.popup(ActionModalStatusEnum.Success,"Debit Card Removed ! ", "Debit Card removed successfully!",4000);
    },(error)=>{
      this._modalService.popup(ActionModalStatusEnum.Error,"Error!", "Something Went Wrong!",4000);
    })
  }
}
