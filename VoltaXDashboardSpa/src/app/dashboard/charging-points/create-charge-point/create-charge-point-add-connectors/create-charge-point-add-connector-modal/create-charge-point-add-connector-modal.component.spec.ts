/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { CreateChargePointAddConnectorModalComponent } from './create-charge-point-add-connector-modal.component';

describe('CreateChargePointAddConnectorModalComponent', () => {
  let component: CreateChargePointAddConnectorModalComponent;
  let fixture: ComponentFixture<CreateChargePointAddConnectorModalComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ CreateChargePointAddConnectorModalComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CreateChargePointAddConnectorModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
