import { Component, Input, OnInit } from '@angular/core';
import { FormArray, FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { ConnectorService } from 'src/_services/connector.service';

@Component({
  selector: 'app-connector-realtime-pricing-access',
  templateUrl: './connector-realtime-pricing-access.component.html',
  styleUrls: ['./connector-realtime-pricing-access.component.sass']
})
export class ConnectorRealtimePricingAccessComponent implements OnInit {

  formArray: FormArray;
  connectors : any[] = [];
  @Input() ChargePoint : any = {};

  constructor(
    private _connectorService : ConnectorService,
    private _fb: FormBuilder,
    private _modalService : ActionModalService
  ) { 
    this.formArray = this._fb.array([]);
  }

  ngOnInit() {
    this.getConnectors();
  }

  getConnectors() {
    this._connectorService.getChargePointConnectors(this.ChargePoint.id).subscribe((data) => {
      this.connectors = data;
      this.createFormGroupsForConnectors();
      console.log("Connectors loaded:", this.connectors);
    });
  }

  updateConnectorPricing(index: number) {
    const updatedData = this.formArray.at(index).value;
    const connectorId = this.connectors[index].id;

    this._connectorService.updateConnectorPricing(connectorId, updatedData).subscribe(response => {
      console.log(`Connector ${connectorId} updated successfully`, response);
      this._modalService.popup(ActionModalStatusEnum.Success, "Success", "Pricing for connector "+ connectorId+" Updated successfully" , 4000)
    }, error => {
      console.error(`Error updating connector ${connectorId}`, error);
      this._modalService.popup(ActionModalStatusEnum.Error, "Error", "Something Went Wrong, please try again later", 4000)
    });
  }

  createFormGroupsForConnectors() {
    this.connectors.forEach((connector) => {
      console.log("this is the connector");
      console.log(connector);
      const group = this._fb.group({
        pricePerKwh: [connector.pricePerKWh || '', Validators.required],
        pricePerMinute: [connector.pricePerMinute || '', Validators.required],
        pricePerHour: [connector.pricePerHour || '', Validators.required]
      });
      this.formArray.push(group);
    });
  }

  getConnectorFormGroup(index: number): FormGroup {
    return this.formArray.at(index) as FormGroup;
  }

}
