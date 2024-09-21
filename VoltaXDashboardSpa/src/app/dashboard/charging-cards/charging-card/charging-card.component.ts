import { Component, OnInit } from '@angular/core';
import { FormGroup, FormControl } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { CardService } from 'src/_services/card.service';
import { EnumMappingService } from 'src/_services/enum-mapping.service';
import { environment } from 'src/environments/environment';

enum ChargingCardTabsEnum {
  InformationsTab = "InformationsTab",
  Transactions = "Transactions",
  Orders = "Orders",
}
@Component({
  selector: 'app-charging-card',
  templateUrl: './charging-card.component.html',
  styleUrls: ['./charging-card.component.sass']
})
export class ChargingCardComponent implements OnInit {
 
  tabsEnum : ChargingCardTabsEnum = ChargingCardTabsEnum.InformationsTab;

  cardID : number = 0;
  cardLoaded : boolean = false;
  CardTypesValues : any = {};
  CardStatusesValues : any = {};
  updateCardObservable = (id : number, model : any) => this._cardService.edit(id, model);

  card: any = {};

  staticUrl : string = environment.apiStaticFilesUrl;

  // Form group
  cardForm : FormGroup;

  constructor(
    private _cardService: CardService,
    private _route: ActivatedRoute,
    private _enumService : EnumMappingService
  ) {
    this.cardForm = new FormGroup({
      serialNumber : new FormControl(''),
      make : new FormControl(''),
      status : new FormControl(''),
      category : new FormControl(''),
      comment : new FormControl(''),
      chargePointCategory : new FormControl(''),
    })
   }

  ngOnInit() {
    var idParam = this._route.snapshot.paramMap.get('id')
    if (idParam != null) {
      var id = parseInt(idParam);
      this.getChargingCardByID(id);
    }
  }


  getChargingCardByID(id : number){
    this.cardID = id;
    this._cardService.getCardByID(id).subscribe((cs) => {
      this.card = cs;
      this.cardLoaded = true;
    })
  }

  changeTab(tab : any){
    this.tabsEnum = tab;
  }

}
