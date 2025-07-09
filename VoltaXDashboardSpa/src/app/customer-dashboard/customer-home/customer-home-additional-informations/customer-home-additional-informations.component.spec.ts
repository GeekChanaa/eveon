/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { CustomerHomeAdditionalInformationsComponent } from './customer-home-additional-informations.component';

describe('CustomerHomeAdditionalInformationsComponent', () => {
  let component: CustomerHomeAdditionalInformationsComponent;
  let fixture: ComponentFixture<CustomerHomeAdditionalInformationsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ CustomerHomeAdditionalInformationsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CustomerHomeAdditionalInformationsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
