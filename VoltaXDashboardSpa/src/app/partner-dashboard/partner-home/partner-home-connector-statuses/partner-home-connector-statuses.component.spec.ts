/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { PartnerHomeConnectorStatusesComponent } from './partner-home-connector-statuses.component';

describe('PartnerHomeConnectorStatusesComponent', () => {
  let component: PartnerHomeConnectorStatusesComponent;
  let fixture: ComponentFixture<PartnerHomeConnectorStatusesComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PartnerHomeConnectorStatusesComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PartnerHomeConnectorStatusesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
