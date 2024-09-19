/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { ChargePointConnectorComponent } from './charge-point-connector.component';

describe('ChargePointConnectorComponent', () => {
  let component: ChargePointConnectorComponent;
  let fixture: ComponentFixture<ChargePointConnectorComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ChargePointConnectorComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ChargePointConnectorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
