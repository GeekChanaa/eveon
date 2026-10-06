import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { ConnectorService } from 'src/_services/connector.service';
import { OcppComponentsService } from 'src/_services/ocpp-components.service';
import { OcppConfigurationService } from 'src/_services/ocpp-services/ocpp-configuration.service';
import { showOcppCommandFeedback } from 'src/_services/ocpp-services/ocpp-command-feedback';

@Component({
  selector: 'app-set-display-message-request',
  templateUrl: './set-display-message-request.component.html',
  styleUrls: ['./set-display-message-request.component.sass']
})
export class SetDisplayMessageRequestComponent implements OnInit {

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
    message : {
      display : {
        evse : {}
      },
      message : {}
    }
  } ;
  isLoading : boolean = false;

  monitorID! : number;

  constructor(
    private _configurationService: OcppConfigurationService,
    private _modalService: ActionModalService,
    private _ocppComponentsService: OcppComponentsService,
    private _connectorService : ConnectorService
  ) { }

  ngOnInit() {
    this.getComponents();
    this.getChargePointConnectors();
  }

  setDisplayMessageRequest(){
    console.log("this is the request");
    console.log(this.request);
    this.request.getVariableData = this.variableDataTypes;
    this.isLoading = true;
    this._configurationService.setDisplayMessage(this.chargePointID, this.request).subscribe((data) => {
      this.isLoading = false;
      if(showOcppCommandFeedback(this._modalService, data)) this.successEvent.emit();
    },(error) => {
      this.isLoading = false;
      showOcppCommandFeedback(this._modalService, error);
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


  onConnectorChange() {
    if (this.selectedConnector) {
      let conn = JSON.parse(this.selectedConnector);
      this.request.message.display.evse.id = conn.connectorID;
      this.request.message.display.evse.connectorId = conn.evseID;
    }
  }

  onComponentChange(){
    this.getComponentInstances(this.variableDataType.component.name);
  }

  getComponentInstances(name : string){
    this._ocppComponentsService.getComponentInstances(name).subscribe((data) => {
      this.componentInstances = data;
    });
  }
}
