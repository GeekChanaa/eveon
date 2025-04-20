/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { CreateChargingStationAddChargePointModalComponent } from './create-charging-station-add-charge-point-modal.component';

describe('CreateChargingStationAddChargePointModalComponent', () => {
  let component: CreateChargingStationAddChargePointModalComponent;
  let fixture: ComponentFixture<CreateChargingStationAddChargePointModalComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ CreateChargingStationAddChargePointModalComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CreateChargingStationAddChargePointModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
