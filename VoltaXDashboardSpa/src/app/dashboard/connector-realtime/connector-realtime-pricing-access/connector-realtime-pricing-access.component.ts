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
  flatFeeFormArray : FormArray;
  connectors : any[] = [];
  @Input() ChargePoint : any = {};

  isLoadingUpdate : any[] = [];
  isLoadingFFUpdate : any[] = [];
  pricingInitialValues: any[] = [];
  flatFeeInitialValues: any[] = [];

  isLoading : boolean = false;
  

  constructor(
    private _connectorService : ConnectorService,
    private _fb: FormBuilder,
    private _modalService : ActionModalService
  ) { 
    this.formArray = this._fb.array([]);
    this.flatFeeFormArray = this._fb.array([]);
  }

  ngOnInit() {
    this.getConnectors();
  }

  getConnectors() {
    this.isLoading = true;
    this._connectorService.getChargePointConnectors(this.ChargePoint.id).subscribe((data) => {
      this.isLoading = false;
      this.connectors = data;
      this.createFormGroupsForConnectors();
      this.createFlatFeeFormGroupsForConnectors();
    },(error) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Error, "Error", "Something Went Wrong, please try again later", 4000)
    });
  }

  updateConnectorPricing(index: number) {
    const updatedData = this.formArray.at(index).value;
    const connectorId = this.connectors[index].id;
    this.isLoadingUpdate[connectorId] = true;

    this._connectorService.updateConnectorPricing(connectorId, updatedData).subscribe(response => {
      this.isLoadingUpdate[connectorId] = false;
      this.pricingInitialValues[index] = updatedData;
      this._modalService.popup(ActionModalStatusEnum.Success, "Success", "Pricing for connector "+ connectorId+" Updated successfully" , 4000)
    }, error => {
      this.isLoadingUpdate[connectorId] = false;
      this._modalService.popup(ActionModalStatusEnum.Error, "Error", "Something Went Wrong, please try again later", 4000)
    });
  }

  updateConnectorFlatFee(index: number) {
    const flatFee = this.flatFeeFormArray.at(index).value;
    const connectorId = this.connectors[index].id;
    this.isLoadingFFUpdate[connectorId] = true;

    this._connectorService.updateConnectorFlateFee(connectorId, flatFee.flatFee).subscribe(response => {
      this.isLoadingFFUpdate[connectorId] = false;
      this.flatFeeInitialValues[index] = flatFee;
      this._modalService.popup(ActionModalStatusEnum.Success, "Success", "Flat fee for connector "+ connectorId+" Updated successfully" , 4000)
    }, error => {
      this.isLoadingFFUpdate[connectorId] = false;
      this._modalService.popup(ActionModalStatusEnum.Error, "Error", "Something Went Wrong, please try again later", 4000)
    });
  }


  createFormGroupsForConnectors() {
    this.formArray.clear();
    this.connectors.forEach((connector, index) => {
      this.isLoadingFFUpdate[connector.id] = false;
      const group = this._fb.group({
        pricePerKwh: [connector.pricePerKWh ?? '', [Validators.required, Validators.min(0)]],
        pricePerIdleMinute: [connector.pricePerIdleMinute ?? '', [Validators.required, Validators.min(0)]],
        costPerKwh: [connector.costPerKwh ?? '', [Validators.required, Validators.min(0)]]
      });
      this.formArray.push(group);
      this.pricingInitialValues[index] = group.value;
    });
  }

  createFlatFeeFormGroupsForConnectors() {
      this.flatFeeFormArray.clear();
      this.connectors.forEach((connector,index) => {
      this.isLoadingUpdate[connector.id] = false;
      const group = this._fb.group({
        flatFee: [connector.flatFee ?? '', [Validators.required, Validators.min(0)]]
      });
      this.flatFeeFormArray.push(group);
      this.flatFeeInitialValues[index] = group.value; 
    });
  }

  getConnectorFormGroup(index: number): FormGroup {
    return this.formArray.at(index) as FormGroup;
  }

  getFlatFeeConnectorFormGroup(index: number): FormGroup {
    return this.flatFeeFormArray.at(index) as FormGroup;
  }

  isConnectorFormGroupDirty(index: number): boolean {
    const formGroup = this.getConnectorFormGroup(index);
    return JSON.stringify(formGroup.value) !== JSON.stringify(this.pricingInitialValues[index]);
  }

  isFlatFeeFormGroupDirty(index: number): boolean {
    const formGroup = this.getFlatFeeConnectorFormGroup(index);
    return JSON.stringify(formGroup.value) !== JSON.stringify(this.flatFeeInitialValues[index]);
  }

  resetPricingAllConnectors(){
    this._connectorService.resetPricingChargePointConnectors(this.ChargePoint.id).subscribe((data)=> {
      this._modalService.popup(ActionModalStatusEnum.Success, "Success", "All connectors pricing reset successfully" , 4000);
      this.getConnectors();
    })
  }

  resetConnectorPricing(connectorID : number){
    this._connectorService.resetPricingConnector(connectorID).subscribe((data)=> {
      this._modalService.popup(ActionModalStatusEnum.Success, "Success", "Connector Pricing Reset Success" , 4000);
      this.getConnectors();
    })
  }

} 
