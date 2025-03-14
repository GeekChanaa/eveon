import { Component, Input, OnInit } from '@angular/core';
import { Pagination } from 'src/_models/pagination';
import { ActionModalService } from 'src/_services/action-modal.service';
import { OCPPConfigurationItemService } from 'src/_services/ocpp-configuration-item.service';

@Component({
  selector: 'app-connector-realtime-ocpp-configuration',
  templateUrl: './connector-realtime-ocpp-configuration.component.html',
  styleUrls: ['./connector-realtime-ocpp-configuration.component.sass']
})
export class ConnectorRealtimeOcppConfigurationComponent implements OnInit {

  ocppConfigurations : any[] = [];
  
  @Input() chargePoint : any = {};

  isHovered : boolean = false;
  stoppingTransactionID? : string;

  paginationPages: any[] = [];
  
  
  pagination: Pagination = {
    currentPage: 0,
    itemsPerPage: 0,
    totalItems: 0,
    totalPages: 0
  }

  itemParams : any = {};

  itemsPerPage: number = 10;
  currentPage: number = 1;

  constructor(
    private _configService : OCPPConfigurationItemService,
    private _modalService : ActionModalService
  ) { }
  
  
    
    ngOnInit() {
      this.getOcppConfigurations();
    }
  
    // Get ChargePoint Transactions
    getOcppConfigurations(page : number = 1){
      this._configService.getChargePointConfigurationItems(this.chargePoint.id,page, this.itemsPerPage, this.itemParams).subscribe((data) => {
        if(data.result)
          this.ocppConfigurations = data.result;
        if(data.pagination)
          this.pagination = data.pagination;
      },(error) => {
      })
    }
  
    closeModal(){
      this.stoppingTransactionID = undefined;
    }
  
}
