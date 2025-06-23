import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { DebitCard } from 'src/_models/debit-card';
import { ActionModalService } from 'src/_services/action-modal.service';
import { DebitCardService } from 'src/_services/debit-card.service';

@Component({
  selector: 'app-add-debit-card-profile',
  templateUrl: './add-debit-card-profile.component.html',
  styleUrls: ['./add-debit-card-profile.component.sass']
})
export class AddDebitCardProfileComponent implements OnInit {

  debitCardForm : FormGroup;
  @Output() cancelEvent : EventEmitter<void> = new EventEmitter();
  @Output() createdEvent : EventEmitter<void> = new EventEmitter();
  @Input() userID : number = 0;

  isLoading : boolean = false;

  constructor(
    private _debitCardService : DebitCardService,
    private _modalService : ActionModalService
  ) { 
    this.debitCardForm = new FormGroup({
      debitCardHolderName: new FormControl('', [Validators.required]),
      debitCardNumber: new FormControl('', [Validators.required, Validators.pattern(/^\d{16}$/)]),
      debitCardExpirationDate: new FormControl('', [Validators.required]),
      debitCardCVV: new FormControl('', [Validators.required, Validators.pattern(/^\d{3}$/)]),
    });
  }

  ngOnInit() {
  }

  cancel(){
    this.cancelEvent.emit();
  }

  // Save Debit Card
  debitCardSave() {
    this.isLoading = true;
    var debitCardValue = this.debitCardForm.value;
    var debitCard: DebitCard = {
      id: 0,
      userID: this.userID,
      cardNumber: debitCardValue.debitCardNumber,
      name: debitCardValue.debitCardHolderName,
      cvv: debitCardValue.debitCardCVV,
      expirationDate: this.convertToDate(
      debitCardValue.debitCardExpirationDate
    ),
    };
    this._debitCardService.create(debitCard).subscribe((data) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Success,"Debit Card Added ! ", "Debit Card added successfully!",4000);
      this.createdEvent.emit();
    },(error)=>{
      this._modalService.popup(ActionModalStatusEnum.Error,"Error!", "Something Went Wrong!",4000);
    });
  }

  convertToDate(dateString: string): Date {
    // Split the string into month and year
    const parts = dateString.split('/');
    const month = parseInt(parts[0], 10);
    const year = parseInt(parts[1], 10);

    return new Date(year, month - 1, 1);
  }

  getControl(name: string): FormControl {
    return this.debitCardForm.get(name) as FormControl;
  }

}
