import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { ConnectorService } from 'src/_services/connector.service';
import { OcppComponentsService } from 'src/_services/ocpp-components.service';
import { OcppReportingService } from 'src/_services/ocpp-services/ocpp-reporting.service';
import { showOcppCommandFeedback } from 'src/_services/ocpp-services/ocpp-command-feedback';

@Component({
  selector: 'app-get-report-request',
  templateUrl: './get-report-request.component.html',
  styleUrls: ['./get-report-request.component.sass']
})
export class GetReportRequestComponent implements OnInit {

  @Input() chargePointID! : string;
  @Output() successEvent : EventEmitter<void> = new EventEmitter();
  @Input() cpID! : number;
  selectedConnector: any = {};
  compCriterias : any[] = ["Active","Available","Enabled","Problem"]; 
  componentVariable : any = {
    component : {
      evse : {}
    },
    variable :{}
  };

  componentVariables : any[] = [];
  CVariables : any[] = [];
  componentCriterias : any[] = [];
  selectedComponentCriterias : any[] =[];

  components : any[] = [];
  connectors : any[] = [];
  componentInstances : any[] = [];

  request : any = {
    componentVariable : [],
    componentCriteria : []
  } ;
  isLoading : boolean = false;

  monitorID! : number;

  constructor(
    private _reportingService: OcppReportingService,
    private _modalService: ActionModalService,
    private _ocppComponentsService: OcppComponentsService,
    private _connectorService : ConnectorService
  ) { }

  ngOnInit() {
    this.componentCriterias = [...this.compCriterias];
    this.getComponents();
    this.getChargePointConnectors();
  }

  getReport(){
    this.request.componentVariable = this.CVariables;
    this.request.componentCriteria = this.selectedComponentCriterias;
    console.log("THIS IS THE REQUEST ======================");
    console.log(this.request);
    this.isLoading = true;
    this._reportingService.getReport(this.chargePointID, this.request).subscribe((data) => {
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

  getComponentVariables(name : string){
    this._ocppComponentsService.getComponentVariables(name).subscribe((data) => {
      this.componentVariables = data;
    })
  }

  onConnectorChange() {
    if (this.selectedConnector) {
      let conn = JSON.parse(this.selectedConnector);
      this.componentVariable.component.evse.id = conn.connectorID;
      this.componentVariable.component.evse.connectorId = conn.evseID;
    }
  }

  onComponentChange(){
    this.getComponentInstances(this.componentVariable.component.name);
    this.getComponentVariables(this.componentVariable.component.name);
  }

  getComponentInstances(name : string){
    this._ocppComponentsService.getComponentInstances(name).subscribe((data) => {
      this.componentInstances = data;
    });
  } 

  addComponentVariable(){
    if(this.componentVariable.component.name == "" || this.componentVariable.component.name == null) return;
    console.log(this.componentVariable);
    this.CVariables.push(this.componentVariable);
    this.componentVariable = {
      component : {
        evse : {}
      },
      variable :{}
    };
  }

  refreshComponentCriterias(){
    this.componentCriterias = this.compCriterias.filter(x => !this.selectedComponentCriterias.includes(x));
  }

  addComponentCriteria(event : any){
    console.log(event);
    let criteria = event.target.value;
    if(criteria == "") return;
    this.selectedComponentCriterias.push(criteria);
    this.refreshComponentCriterias();
  }

  removeComponentCriteria(criteria : string){
    this.selectedComponentCriterias = this.selectedComponentCriterias.filter(x => x != criteria);
    this.refreshComponentCriterias();
  }



}
