import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ChargePointCategoryEnum } from 'src/_models/_enums/charge-point-category';
import { ChargePointStatusEnum } from 'src/_models/_enums/charge-point-status';
import { ChargePoint } from 'src/_models/charge-point';
import { ActionModalService } from 'src/_services/action-modal.service';
import { ConnectorService } from 'src/_services/connector.service';
import { EnumMappingService } from 'src/_services/enum-mapping.service';
import { ConfirmService } from 'src/_services/confirm.service';

@Component({
  selector: 'app-charge-point-connector',
  templateUrl: './charge-point-connector.component.html',
  styleUrls: ['./charge-point-connector.component.sass']
})
export class ChargePointConnectorComponent implements OnInit {

  @Output() refreshEvent : EventEmitter<number> = new EventEmitter<number>();
  @Input() connectorID : number = 0;

  editingConnector : number = 0;
  loading = true;
  loadError = false;
  deleting = false;

  chargePointCategory : string = "";
  chargePointStatus : string = "";

  @Input() connector : any = {};

  constructor(
    private _connectorService : ConnectorService,
    private _enumMappings : EnumMappingService,
    private _modalService : ActionModalService,
    private _confirmService : ConfirmService
  ) { }

  ngOnInit() {
    this.chargePointCategory = this._enumMappings.getEnumMapping("ChargePointCategory")[this.connector.category];
    this.getConnector();
  }

  requestDeleteConnector(): void {
    if (this.deleting) return;
    const connectorLabel = this.connector.connectorID ?? this.connectorID;
    const evse = this.connector.evseID ? ` on EVSE ${this.connector.evseID}` : '';
    this._confirmService.requestConfirmation(
      `Delete connector ${connectorLabel}?`,
      `This will remove connector ${connectorLabel}${evse} from this charge point. This action cannot be undone.`,
      () => this.deleteConnector(),
      { confirmLabel: 'Delete connector', tone: 'danger' }
    );
  }

  private deleteConnector(): void {
    if (this.deleting) return;
    this.deleting = true;
    this._connectorService.deleteById(this.connectorID).subscribe((data) => {
      this._modalService.popup(ActionModalStatusEnum.Success, "Deleted", "The Connector : "+ this.connector.connectorID + " was deleted succesfully", 4000);
      this.refreshEvent.emit();
    },(error) => {
      this.deleting = false;
      this._modalService.popup(ActionModalStatusEnum.Error,"Error !","Something went wrong",4000);
    })
  }

  getConnector(){
    this.loading = true;
    this.loadError = false;
    this._connectorService.getById(this.connectorID).subscribe({
      next: data => { this.connector = data; this.loading = false; },
      error: () => { this.loading = false; this.loadError = true; }
    });
  }

  editConnector(id : number){
    this.editingConnector = id;
  }


}
