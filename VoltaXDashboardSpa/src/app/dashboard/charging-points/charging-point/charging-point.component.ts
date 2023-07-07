import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ChargePointCategoryEnum } from 'src/_models/_enums/charge-point-category';
import { ChargePointStatusEnum } from 'src/_models/_enums/charge-point-status';
import { ChargePoint } from 'src/_models/charge-point';
import { ChargePointService } from 'src/_services/charge-point.service';
import { ConnectorService } from 'src/_services/connector.service';

enum ChargePointTabsEnum {
  InformationsTab = "InformationsTab",
  ConnectorsTab = "ConnectorsTab"
}

@Component({
  selector: 'app-charging-point',
  templateUrl: './charging-point.component.html',
  styleUrls: ['./charging-point.component.css']
})
export class ChargingPointComponent implements OnInit {

  // TabsEnum
  tabsEnum : ChargePointTabsEnum = ChargePointTabsEnum.InformationsTab;

  // charge point
  chargePoint : ChargePoint = {
    id: 0,
    chargePointId: '',
    chargingStationID: 0,
    name: '',
    serialNumber: '',
    make: '',
    status: ChargePointStatusEnum.Available,
    comment: '',
    username: '',
    password: '',
    clientCertThumb: '',
    connectors: [],
    transactions: [],
    category: ChargePointCategoryEnum.TheTower
  }

  chargePointID : number = 0;

  constructor(
    private _chargePointService : ChargePointService,
    private _route : ActivatedRoute,
    private _connectorService : ConnectorService
  ) { }

  ngOnInit() {
    var idParam = this._route.snapshot.paramMap.get('id')
    if (idParam != null) {
      var id = parseInt(idParam);
      this.chargePointID = id;
      this._chargePointService.getById(id).subscribe((cs) => {
        this.chargePoint = cs;
      })
    }
  }

  // Changing current tab
  changeTab(tab : any){
    this.tabsEnum = tab;
  }

}
