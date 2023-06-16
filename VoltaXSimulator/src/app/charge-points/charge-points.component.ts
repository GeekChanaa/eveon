import { Component, OnInit } from '@angular/core';
import { ChargePoint } from 'src/_models/charge-point';
import { Connector } from 'src/_models/connector';
import { ChargePointService } from 'src/_services/charge-point.service';

@Component({
  selector: 'app-charge-points',
  templateUrl: './charge-points.component.html',
  styleUrls: ['./charge-points.component.css']
})
export class ChargePointsComponent implements OnInit {

  chargePointId = '';
  chargeStationId = '';
  chargePointName = '';
  chargePointGateway = '';
  connectors: Connector[] = [];
  editIndex = -1;

  constructor(public pointService: ChargePointService) {}

  ngOnInit(): void {}

  addOrUpdateChargePoint() {
    const point = new ChargePoint(this.chargePointId, this.chargeStationId, this.chargePointName, this.chargePointGateway, this.connectors);
    if (this.editIndex === -1) {
      this.pointService.addChargePoint(point);
    } else {
      this.pointService.updateChargePoint(this.editIndex, point);
      this.editIndex = -1;
    }
    this.clearForm();
  }

  addConnector(connectorId: string, connectorType: string) {
    if (this.connectors.length < 3) {
      this.connectors.push(new Connector(this.chargePointId, connectorId, connectorType));
    } else {
      alert('Max 3 connectors per charge point!');
    }
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
    this.chargePointGateway = point.chargePointGateway;
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

}
