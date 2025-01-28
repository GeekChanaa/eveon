import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { ConnectorService } from 'src/_services/connector.service';
import { OcppComponentsService } from 'src/_services/ocpp-components.service';
import { OcppMonitoringService } from 'src/_services/ocpp-services/ocpp-monitoring.service';

@Component({
  selector: 'app-get-variables-request',
  templateUrl: './get-variables-request.component.html',
  styleUrls: ['./get-variables-request.component.sass']
})
export class GetVariablesRequestComponent implements OnInit {

  @Input() chargePointID! : string;
  @Output() successEvent : EventEmitter<void> = new EventEmitter();
  @Input() cpID! : number;
  selectedConnector: any = {};
  variableDataType : any = {
    component : {
      evse : {}
    },
    variable :{}
  };

  variableDataTypes : any[] =[];

  components : any[] = [];
  connectors : any[] = [];
  componentInstances : any[] = [];
  componentVariables : any[] = [];

  request : any = {
    getVariableData : []
  } ;
  isLoading : boolean = false;

  monitorID! : number;

  constructor(
    private _monitoringService: OcppMonitoringService,
    private _modalService: ActionModalService,
    private _ocppComponentsService: OcppComponentsService,
    private _connectorService : ConnectorService
  ) { }

  ngOnInit() {
    this.getComponents();
    this.getChargePointConnectors();
  }

  getVariablesRequest(){
    console.log(this.request);
    this.request.getVariableData = this.variableDataTypes;
    this.isLoading = true;
    this._monitoringService.getVariables(this.chargePointID, this.request).subscribe((data) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Success,"Success !", "Request Sent successfully ! ",4000);
      this.successEvent.emit();
    },(error) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Error, "Error !","Something went wrong please contact your system administrator",4000);
    })
  }


  getComponents(){
    this.isLoading = true;
    this._ocppComponentsService.getComponents().subscribe((data) => {
      this.isLoading = false
      this.components = data;
    },(error) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Error, "Error !","Something went wrong please contact your system administrator",4000);
    })
  }

  getChargePointConnectors(){
    this._connectorService.getChargePointConnectors(this.cpID).subscribe((data) => {
      this.connectors = data;
    })
  }

  getComponentVariables(name : string){
    this._ocppComponentsService.getComponentVariables(name).subscribe((data) => {
      console.log("this is the component variables");
      console.log(this.componentVariables);
      this.componentVariables = data;
    })
  }

  onConnectorChange() {
    if (this.selectedConnector) {
      let conn = JSON.parse(this.selectedConnector);
      this.variableDataType.component.evse.id = conn.connectorID;
      this.variableDataType.component.evse.connectorId = conn.evseID;
    }
  }

  onComponentChange(){
    this.getComponentInstances(this.variableDataType.component.name);
    this.getComponentVariables(this.variableDataType.component.name);
  }

  getComponentInstances(name : string){
    this._ocppComponentsService.getComponentInstances(name).subscribe((data) => {
      this.componentInstances = data;
    });
  } 

  addVariableType(){
    console.log(this.variableDataType);
    this.variableDataTypes.push(this.variableDataType);
    this.variableDataType = {
      component : {
        evse : {}
      },
      variable :{}
    };
    
  }


}
