import { AfterViewInit, Component, ElementRef, OnInit, Renderer2 } from '@angular/core';
import { FormArray, FormControl, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { CardStatusEnum } from 'src/_models/_enums/card-status';
import { CardTypeEnum } from 'src/_models/_enums/card-type';
import { Card } from 'src/_models/card';
import { ActionModalService } from 'src/_services/action-modal.service';
import { AuthService } from 'src/_services/auth.service';
import { CardService } from 'src/_services/card.service';
import { ChargePointService } from 'src/_services/charge-point.service';
import { ChargingStationService } from 'src/_services/charging-station.service';
import { EnumMappingService } from 'src/_services/enum-mapping.service';
import { UserService } from 'src/_services/user.service';
declare var $: any;  

@Component({
  selector: 'app-create-charging-card',
  templateUrl: './create-charging-card.component.html',
  styleUrls: ['./create-charging-card.component.sass']
})
export class CreateChargingCardComponent implements OnInit, AfterViewInit {

  form : FormGroup;
  card : any = {};
  users : any[] = [];

  ngAfterViewInit() {
  }

  get userControl(): FormControl {
    const control = this.form.get('userID');
    if (!control) {
      throw new Error('User control not found');
    }
    return control as FormControl;
  }

  ngOnInit() {
    this.cardTypes = Object.values(this._enumService.getEnumMapping("ChargingStationCategoryEnum"));
    this.cardStatuses = Object.values(this._enumService.getEnumMapping("ChargingStationCategoryEnum"));
    this.getAllUserNames();
  }


  updateUser(user : any){
    this.userControl.setValue(user.id);
  }

  opacity: number = 0;
  activeDiv = 1;


  cardTypes : any = {};
  cardStatuses : any = {};



  constructor(
    private _enumService : EnumMappingService,
    private _modalService:  ActionModalService,
    private _router : Router,
    private _cardService : CardService,
    private _userService: UserService
  ) {
    this.form = new FormGroup({
      cardType : new FormControl('Standard'),
      status : new FormControl('Active'),
      balance : new FormControl('0',[Validators.pattern('^[0-9]*$')] ),
      note : new FormControl(''),
      userID : new FormControl('')
    })
  }

  get chargePoints() {
    return this.form.get('chargePoints') as FormArray;
  }

  getChargePointConnectors() {
    return (this.form.get('chargePointConnectors') as FormArray);
  }

  addChargePointConnector() {
    (this.form.get('chargePointConnectors') as FormArray).push(new FormGroup({
      chargePointConnectorSpeed: new FormControl('7.3'),
      chargePointConnectorPricePerKWh : new FormControl(""),
      chargePointConnectorPricePerMinute : new FormControl(""),
      chargePointConnectorPricePerHour : new FormControl("")
    }));
  }


  showSelect() {
    this.opacity = 1;
  }

  hideSelect() {
    this.opacity = 0;
  }

  getFormControl(name: string): FormControl {
    return this.form.get(name) as FormControl;
  }

  
  onSubmit(){
    var cardForm = this.form.value;
    this.card.cardType = cardForm.cardType;
    this.card.status = cardForm.status;
    this.card.note = cardForm.note;
    this.card.userID = cardForm.userID;
    this.card.balance = cardForm.balance;

    console.log("this is the card we're pushing");
    console.log(this.card);

    this._cardService.createCard(this.card).subscribe((createdCard) => {
      this._modalService.popup(ActionModalStatusEnum.Success,"Succcess !","Card Created Successfully",4000);
      this._router.navigateByUrl('/dashboard/charging-cards');
    },(error) => {
      this._modalService.popup(ActionModalStatusEnum.Error,"Error","Something went wrong",4000);
    });
  }

  

  getAllUserNames(){
    this._userService.getUserNames().subscribe((data) => {
      this.users = data;
    })
  }

  getAllUsersNamesByName(name : string){
    this._userService.getAllUsersNamesByName(name).subscribe((data) => {
      this.users = data;
    })
  }

}
