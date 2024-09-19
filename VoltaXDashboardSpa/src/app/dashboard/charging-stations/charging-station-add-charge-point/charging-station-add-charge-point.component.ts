import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormGroup, FormControl } from '@angular/forms';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { ChargePointService } from 'src/_services/charge-point.service';

@Component({
  selector: 'app-charging-station-add-charge-point',
  templateUrl: './charging-station-add-charge-point.component.html',
  styleUrls: ['./charging-station-add-charge-point.component.sass']
})
export class ChargingStationAddChargePointComponent implements OnInit {

  chargePointForm : FormGroup;
  @Input() chargingStationID : number = 0;
  @Output() successEvent : EventEmitter<void> = new EventEmitter();
  @Output() cancelEvent : EventEmitter<void> = new EventEmitter();

  chargePointIDTouched : boolean = false;
  chargePointSerialNumberTouched : boolean = false;
  checkingChagePointID : boolean = false;
  checkingChargePointSerialNumber : boolean = false;
  chargePointTimeout: any = {};
  chargePointSerialNumberTimeout: any = {};
  chargePointExist : boolean = false;
  chargePointSerialNumberExist : boolean = false;

  constructor(
    private _chargePointService: ChargePointService,
    private _modalService:  ActionModalService
  ) {
    this.chargePointForm = new FormGroup({
      serialNumber : new FormControl(''),
      name : new FormControl(''),
      username : new FormControl(''),
      password : new FormControl(''),
      clientCertThumb : new FormControl(''),
      chargePointID: new FormControl(''),
      status : new FormControl('Available'),
      category : new FormControl('TheTower'),
      comment : new FormControl(''),
      make : new FormControl(''),
      chargePointCategory : new FormControl(''),
    })  
   }

  ngOnInit() {
    this.chargePointForm.get('serialNumber')?.valueChanges.subscribe((data) => {
      this.isChargePointSerialNumberUnique(data);
    })
    this.chargePointForm.get('chargePointID')?.valueChanges.subscribe((data) => {
      this.isChargePointIDUnique(data);
    })
  }

  cpfOnSubmit(){
    var cpf = this.chargePointForm.value;
    const chargePoint : any = {
      name: cpf.name,
      serialNumber: cpf.serialNumber,
      category : cpf.category,
      make: cpf.make,
      status: cpf.status,
      comment: cpf.comment,
      username: cpf.username,
      password: cpf.password,
      clientCertThumb: cpf.clientCertThumb,
      chargePointId: cpf.chargePointID,
      chargingStationID: this.chargingStationID
    }
    this._chargePointService.create(chargePoint).subscribe((data) => {
      this._modalService.popup(ActionModalStatusEnum.Success, "Success", "The Charge Point has been created succesfully", 4000);
      this.successEvent.emit();
    },(error) => {
      this._modalService.popup(ActionModalStatusEnum.Error, "Error", "Something Went Wrong", 4000);
    })
  }

  getControl(name: string): FormControl {
    return this.chargePointForm.get(name) as FormControl;
  }

  isChargePointIDUnique(chargePointID: string): void {
    this.chargePointIDTouched = true;
    this.checkingChagePointID = true;
    clearTimeout(this.chargePointTimeout);
    this.chargePointTimeout = setTimeout(() => {
      this._chargePointService.isChargePointIDUnique(chargePointID).subscribe(
        (data) => {
          this.chargePointExist = data;
          this.checkingChagePointID = false;
        },
        (error) => {
          clearTimeout(this.chargePointTimeout);
        }
      );
    }, 800);
  }


  isChargePointSerialNumberUnique(chargePointSerialNumber: string): void {
    this.chargePointSerialNumberTouched = true;
    this.checkingChargePointSerialNumber = true;
    clearTimeout(this.chargePointSerialNumberTimeout);
    this.chargePointSerialNumberTimeout = setTimeout(() => {
      this._chargePointService.isChargePointSerialNumberUnique(chargePointSerialNumber).subscribe(
        (data) => {
          this.chargePointSerialNumberExist = data;
          this.checkingChargePointSerialNumber = false;
        },
        (error) => {
          clearTimeout(this.chargePointSerialNumberTimeout);
        }
      );
    }, 800);
  }
}
