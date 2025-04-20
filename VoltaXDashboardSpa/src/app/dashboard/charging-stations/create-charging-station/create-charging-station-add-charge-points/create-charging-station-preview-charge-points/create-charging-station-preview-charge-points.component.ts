import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';

@Component({
  selector: 'app-create-charging-station-preview-charge-points',
  templateUrl: './create-charging-station-preview-charge-points.component.html',
  styleUrls: ['./create-charging-station-preview-charge-points.component.sass']
})
export class CreateChargingStationPreviewChargePointsComponent implements OnInit {
  ngOnInit() {
  }

  @Input() chargePoints: any[] = [];
  
  @Output() editChargePoint = new EventEmitter<number>();
  @Output() deleteChargePoint = new EventEmitter<number>();
  @Output() addConnector = new EventEmitter<number>();
  @Output() editConnector = new EventEmitter<{chargePointIndex: number, connectorIndex: number}>();
  @Output() deleteConnector = new EventEmitter<{chargePointIndex: number, connectorIndex: number}>();

  openEditChargePointModal(index: number): void {
    this.editChargePoint.emit(index);
  }

  deleteChargePointHandler(index: number): void {
    this.deleteChargePoint.emit(index);
  }

  openAddConnectorModal(index: number): void {
    this.addConnector.emit(index);
  }

  openEditConnectorModal(chargePointIndex: number, connectorIndex: number): void {
    this.editConnector.emit({ chargePointIndex, connectorIndex });
  }

  deleteConnectorHandler(chargePointIndex: number, connectorIndex: number): void {
    this.deleteConnector.emit({ chargePointIndex, connectorIndex });
  }

  canAddConnector(chargePoint: any): boolean {
    const maxConnectors = chargePoint.category === 'Single' ? 1 : 2;
    return (chargePoint.connectors?.length || 0) < maxConnectors;
  }


}
