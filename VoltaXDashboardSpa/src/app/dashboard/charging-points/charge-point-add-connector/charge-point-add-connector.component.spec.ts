/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { ChargePointAddConnectorComponent } from './charge-point-add-connector.component';

describe('ChargePointAddConnectorComponent', () => {
  let component: ChargePointAddConnectorComponent;
  let fixture: ComponentFixture<ChargePointAddConnectorComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ChargePointAddConnectorComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ChargePointAddConnectorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
