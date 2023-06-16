import { Injectable } from '@angular/core';
import { ChargePoint } from 'src/_models/charge-point';

@Injectable({
  providedIn: 'root'
})
export class ChargePointService {

  chargePoints: ChargePoint[] = [];

  constructor() {
    this.loadChargePoints();
  }

  loadChargePoints() {
    const chargePoints = localStorage.getItem('chargePoints');
    this.chargePoints = chargePoints ? JSON.parse(chargePoints) : [];
  }

  saveChargePoints() {
    localStorage.setItem('chargePoints', JSON.stringify(this.chargePoints));
  }

  getChargePoints() {
    return this.chargePoints;
  }

  addChargePoint(chargePoint: ChargePoint) {
    if (this.isGatewayUnique(chargePoint.chargePointGateway)) {
      this.chargePoints.push(chargePoint);
      this.saveChargePoints();
    } else {
      alert('Charge Point Gateway must be unique!');
    }
  }

  isGatewayUnique(gateway: string) {
    return !this.chargePoints.some(point => point.chargePointGateway === gateway);
  }

  updateChargePoint(index: number, chargePoint: ChargePoint) {
    if (this.isGatewayUnique(chargePoint.chargePointGateway) || 
        this.chargePoints[index].chargePointGateway === chargePoint.chargePointGateway) {
      this.chargePoints[index] = chargePoint;
      this.saveChargePoints();
    } else {
      alert('Charge Point Gateway must be unique!');
    }
  }

  deleteChargePoint(index: number) {
    this.chargePoints.splice(index, 1);
    this.saveChargePoints();
  }

}
