import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { CardService } from 'src/_services/card.service';
import { OcppEvDriverService } from 'src/_services/ocpp-services/ocpp-ev-driver.service';

@Component({
  selector: 'app-request-start-transaction',
  templateUrl: './request-start-transaction.component.html',
  styleUrls: ['./request-start-transaction.component.sass']
})
export class RequestStartTransactionComponent implements OnInit {

  @Input() chargePointID : string = "";
  @Output() successEvent : EventEmitter<void> = new EventEmitter();

  cards : any[] = [];
  tokenID : number = 0;
  remoteStartID : number = 0;
  constructor(
    private _cardService: CardService,
    private _evDriverService : OcppEvDriverService,
    private _modalService: ActionModalService
  ) { }

  ngOnInit() {
    this.getCards();
  }

  getCards(){
    this._cardService.getAllCards(1,-1).subscribe((data) => {
      if(data.result)
        this.cards = (data.result);
    })
  }

  startTransaction(){
    let startTransactionRequest = {
      remoteStartID : this.remoteStartID ,
      idToken : {
        idToken : this.tokenID
      }
    };
    this._evDriverService.requestStartTransaction(this.chargePointID, startTransactionRequest).subscribe((data) => {
      this._modalService.popup(ActionModalStatusEnum.Success,"Success","Transaction Started Successfully",4000);
      this.successEvent.emit()
    },(error)=> {
      this._modalService.popup(ActionModalStatusEnum.Error,"Something Went Wrong","Something Went wrong please try again later", 4000);
    })
  }

}
