import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ChargePointCategoryEnum } from 'src/_models/_enums/charge-point-category';
import { ChargePointStatusEnum } from 'src/_models/_enums/charge-point-status';
import { ChargePoint } from 'src/_models/charge-point';
import { ChargingStation } from 'src/_models/charging-station';
import { ActionModalService } from 'src/_services/action-modal.service';
import { ChargePointService } from 'src/_services/charge-point.service';
import { EnumMappingService } from 'src/_services/enum-mapping.service';

@Component({
  selector: 'app-charging-station-charge-point',
  templateUrl: './charging-station-charge-point.component.html',
  styleUrls: ['./charging-station-charge-point.component.sass']
})
export class ChargingStationChargePointComponent implements OnInit {

  @Output() refreshEvent : EventEmitter<number> = new EventEmitter<number>();
  @Input() chargePointID : number = 0;
  showDeleteConfirmModal : boolean = false;

  chargePointCategory : string = "";
  chargePointStatus : string = "";

  @Input() chargePoint : ChargePoint = {
    id: 0,
    chargePointId: '',
    chargingStationID: 0,
    name: '',
    serialNumber: '',
    make: '',
    status: ChargePointStatusEnum.Available,
    comment: '',
    username: '',
    hasPassword: false,
    clientCertThumb: '',
    connectors: [],
    transactions: [],
    category: ChargePointCategoryEnum.Single
  }

  constructor(
    private _chargePointService : ChargePointService,
    private _enumMappings : EnumMappingService,
    private _modalService : ActionModalService
  ) { }

  ngOnInit() {
    this.chargePointCategory = this._enumMappings.getEnumMapping("ChargePointCategory")[this.chargePoint.category];
    this.chargePointStatus = this._enumMappings.getEnumMapping("ChargePointStatus")[this.chargePoint.status];
    this.getChargePoint();
  }

  deleteChargePoint(){
    this._chargePointService.deleteById(this.chargePointID).subscribe((data) => {
      this._modalService.popup(ActionModalStatusEnum.Success, "Deleted", "The Charge Point : "+ this.chargePoint.chargePointId + " was deleted succesfully", 4000);
      this.refreshEvent.emit();
    },(error) => {
      this._modalService.popup(ActionModalStatusEnum.Error,"Error !","Something went wrong",4000);
      this.refreshEvent.emit();
    })
  }

  getChargePoint(){
    this._chargePointService.getById(this.chargePointID).subscribe((data) => {
      this.chargePoint = data;
    })
  }

  confirmDelete(): void {
    this.showDeleteConfirmModal = true;
  }

  cancelDelete(): void {
    this.showDeleteConfirmModal = false;
  }

  proceedWithDelete(): void {
    this.deleteChargePoint();
    this.showDeleteConfirmModal = false;
  }

}
