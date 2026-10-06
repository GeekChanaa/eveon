import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { map } from 'rxjs';
import { ChargePointCRListDto } from 'src/_models/_dtos/charge-point-cr-list-dto';
import { ChargePointCategoryEnum } from 'src/_models/_enums/charge-point-category';
import { ChargePointStatusEnum } from 'src/_models/_enums/charge-point-status';
import { ChargePoint } from 'src/_models/charge-point';
import { ChargePointService } from 'src/_services/charge-point.service';
import { ocppVersionLabel } from 'src/_services/ocpp-services/ocpp-configuration.service';
import { ConfirmService } from 'src/_services/confirm.service';
import { ActionModalService } from 'src/_services/action-modal.service';
import { AccessService } from 'src/_services/access.service';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';

@Component({
  selector: 'app-charge-points-list',
  templateUrl: './charge-points-list.component.html',
  styleUrls: ['./charge-points-list.component.sass']
})
export class ChargePointsListComponent implements OnInit {

  cities : any[] = [];
  regeneratingQrCodes = false;
  fields: string[] = [];
  filters : any = {
    category:"",
    city : ""
  };
  

  chargePoint: ChargePointCRListDto = {
    id: 0,
    chargePointId: '',
    chargingStationName: '',
    serialNumber: '',
    category: '',
    status: '',
    partnerName: '',
    connection: '',
    ocpp: '',
    configuration: ''
  }

  // Constructor
  constructor(
    private _chargePointService: ChargePointService,
    private _router : Router,
    private _confirmService: ConfirmService,
    private _modalService: ActionModalService,
    public access: AccessService
  ) { }

  ngOnInit() {
    this._getItemFields();
  }

  getChargePointsObservable = (currentPage:  number | undefined, itemsPerPage : number | undefined, itemParams : any) =>
    this._chargePointService.getAllChargePoints(currentPage, itemsPerPage, itemParams).pipe(map(page => {
      // The endpoint returns ChargePointCRListDto rows, not full charge points.
      (page.result as unknown as ChargePointCRListDto[] | undefined)?.forEach(cp => {
        cp.connection = cp.isOnline ? 'Online' : 'Offline';
        cp.ocpp = ocppVersionLabel(cp.protocolVersion) ?? '';
        cp.configuration = this.configurationLabel(cp);
      });
      return page;
    }));

  /** Charge points never configured by VoltaX get a Configure button that opens the guided setup. */
  needsSetup(cp: ChargePointCRListDto): boolean {
    return cp?.isConfigured === false;
  }

  private configurationLabel(cp: ChargePointCRListDto): string {
    if (cp.isConfigured) return 'Configured';
    switch (cp.configurationMethod) {
      case 'Skipped': return 'Accepted as-is';
      case 'Legacy': return 'Not configured';
      default: return 'Pending setup';
    }
  }
  deleteChargePointObservable = (id : number) => this._chargePointService.deleteById(id);
  updateChargePointObservable = (id : number, model : any) => this._chargePointService.edit(id, model);

  private _getItemFields() {
    if (!this.chargePoint || this.chargePoint == undefined) {
      return;
    }
    Object.keys(this.chargePoint ?? {}).forEach((element: string) => {
      if (typeof this.chargePoint?.[element] == "object" && this.chargePoint?.[element] != null && this.chargePoint?.[element].constructor.name == "Date")
        this.fields.push(element);
      if (typeof this.chargePoint?.[element] != "object") this.fields.push(element);
    });
  }
  

  requestRegenerateAllQrCodes(){
    if (this.regeneratingQrCodes) return;
    this._confirmService.requestConfirmation(
      'Regenerate all QR codes?',
      'A new QR code will be generated for every charge point. All previously printed codes will stop working and must be replaced.',
      () => this.regenerateAllQrCodes(),
      { confirmLabel: 'Regenerate all', tone: 'danger' }
    );
  }

  private regenerateAllQrCodes(){
    this.regeneratingQrCodes = true;
    this._chargePointService.regenerateAllQrCodes().subscribe({
      next: result => {
        this.regeneratingQrCodes = false;
        this._modalService.popup(ActionModalStatusEnum.Success, "Regenerated", result.count + " QR codes were regenerated successfully", 4000);
      },
      error: () => {
        this.regeneratingQrCodes = false;
        this._modalService.popup(ActionModalStatusEnum.Error, "Error !", "Could not regenerate the QR codes", 4000);
      }
    });
  }

  resetFilters(){
    this.filters = {
      category:"",
      city : ""
    }
  }
}
