import { Component, OnInit } from '@angular/core';
import { ChargePoint } from 'src/_models/charge-point';
import { Connector } from 'src/_models/connector';
import { ChargePointApiService } from 'src/_services/charge-point-api.service';
import { ChargePointService } from 'src/_services/charge-point.service';

@Component({
  selector: 'app-charge-points',
  templateUrl: './charge-points.component.html',
  styleUrls: ['./charge-points.component.css']
})
export class ChargePointsComponent implements OnInit {

  // Selected charge point
  chargePointId = '';
  chargeStationId = '';
  chargePointName = '';
  chargePointGateway = '';
  selectedId : number = 0;
  connectors: any[] = [];
  editIndex = -1;
  chargePoints : any[] = [];

  constructor(
    public pointService: ChargePointService,
    private _chargePointService: ChargePointApiService
    ) {}

  ngOnInit(): void {
    this.getChargePoints();
  }

  addOrUpdateChargePoint() {
    const point = new ChargePoint(this.selectedId,this.chargePointId, this.chargeStationId, this.chargePointName, this.chargePointGateway, this.connectors);
    if (this.editIndex === -1) {
      console.log("this is the charge point ot be added");
      console.log(point);
      this.pointService.addChargePoint(point);
    } else {
      this.pointService.updateChargePoint(this.editIndex, point);
      this.editIndex = -1;
    }
    this.clearForm();
  }

  deleteConnector(index: number) {
    this.connectors.splice(index, 1);
  }

  deleteChargePoint(index: number) {
    this.pointService.deleteChargePoint(index);
  }

  editChargePoint(index: number) {
    const point = this.pointService.getChargePoints()[index];
    this.chargePointId = point.chargePointId;
    this.chargeStationId = point.chargeStationId;
    this.chargePointName = point.chargePointName;
    this.chargePointGateway = "wss://localhost:7282/OCPP/"+point.chargePointId;
    this.connectors = point.connectors;
    this.editIndex = index;
  }

  clearForm() {
    this.chargePointId = '';
    this.chargeStationId = '';
    this.chargePointName = '';
    this.chargePointGateway = '';
    this.connectors = [];
  }

  // Getting chargepoint from the db
  getChargePoints(){
    this._chargePointService.getAll().subscribe((data) => {
      console.log("these are the chargepoitns");
      if(data.result)
      this.chargePoints = data.result;
      console.log(this.chargePoints);
    })
  }

  // Get charge point connectors
  getChargePointConnectors(chargePoint : any){
    this.selectedId = chargePoint.id;
    this.chargePointGateway = "wss://localhost:7282/OCPP/"+chargePoint.chargePointId;
    
    this._chargePointService.getChargePointConnectors(chargePoint.id).subscribe((data) => {
      this.connectors = data;
      console.log("connectors : ");
      console.log(this.connectors);
    })
  }
}
