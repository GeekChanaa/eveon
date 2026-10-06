import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { AddDebitCardRequest, DebitCardBrand } from 'src/_models/debit-card';
import { ActionModalService } from 'src/_services/action-modal.service';
import { DebitCardService } from 'src/_services/debit-card.service';
import { environment } from 'src/environments/environment';

/**
 * Card numbers and CVVs never enter this application: real card entry belongs to the payment
 * provider's hosted fields, which return a token. Until a provider is integrated, production
 * shows a notice and development builds simulate the tokenization step (display data only).
 */
@Component({
  selector: 'app-add-debit-card-profile',
  templateUrl: './add-debit-card-profile.component.html',
  styleUrls: ['./add-debit-card-profile.component.sass']
})
export class AddDebitCardProfileComponent implements OnInit {

  readonly simulateTokenization = !environment.production;

  debitCardForm : FormGroup;
  @Output() cancelEvent : EventEmitter<void> = new EventEmitter();
  @Output() createdEvent : EventEmitter<void> = new EventEmitter();
  @Input() userID : number = 0;

  isLoading : boolean = false;

  brandOptions = [
    { value: 'Visa', label: 'Visa' },
    { value: 'Mastercard', label: 'Mastercard' },
    { value: 'Generic', label: 'Other' }
  ];

  constructor(
    private _debitCardService : DebitCardService,
    private _modalService : ActionModalService
  ) {
    this.debitCardForm = new FormGroup({
      debitCardHolderName: new FormControl('', [Validators.maxLength(100)]),
      debitCardBrand: new FormControl('Visa', [Validators.required]),
      debitCardLast4: new FormControl('', [Validators.required, Validators.pattern(/^\d{4}$/)]),
      debitCardExpirationDate: new FormControl('', [Validators.required, Validators.pattern(/^(0[1-9]|1[0-2])\/\d{2}$/)]),
    });
  }

  ngOnInit() {
  }

  cancel(){
    this.cancelEvent.emit();
  }

  debitCardSave() {
    if (!this.simulateTokenization || this.debitCardForm.invalid) return;
    this.isLoading = true;
    const value = this.debitCardForm.value;
    const [month, year] = String(value.debitCardExpirationDate).split('/').map(part => parseInt(part, 10));
    const request: AddDebitCardRequest = {
      providerToken: this.simulatedProviderToken(),
      brand: value.debitCardBrand as DebitCardBrand,
      last4: value.debitCardLast4,
      expiryMonth: month,
      expiryYear: 2000 + year,
      name: value.debitCardHolderName || undefined
    };
    this._debitCardService.addCard(request).subscribe(() => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Success,"Debit Card Added ! ", "Debit Card added successfully!",4000);
      this.createdEvent.emit();
    },(error)=>{
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Error,"Error!", error?.error?.error || "Something Went Wrong!",4000);
    });
  }

  /** Stands in for the token a payment provider would return; only accepted by a Development API. */
  private simulatedProviderToken(): string {
    const bytes = new Uint8Array(16);
    crypto.getRandomValues(bytes);
    return 'devtok_' + Array.from(bytes, b => b.toString(16).padStart(2, '0')).join('');
  }

  getControl(name: string): FormControl {
    return this.debitCardForm.get(name) as FormControl;
  }

}
