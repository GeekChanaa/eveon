/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { CreateChargingStationPreviewChargePointsComponent } from './create-charging-station-preview-charge-points.component';

describe('CreateChargingStationPreviewChargePointsComponent', () => {
  let component: CreateChargingStationPreviewChargePointsComponent;
  let fixture: ComponentFixture<CreateChargingStationPreviewChargePointsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ CreateChargingStationPreviewChargePointsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CreateChargingStationPreviewChargePointsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
