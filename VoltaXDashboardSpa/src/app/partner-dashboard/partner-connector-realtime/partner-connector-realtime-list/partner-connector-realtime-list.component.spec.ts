/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { PartnerConnectorRealtimeListComponent } from './partner-connector-realtime-list.component';

describe('PartnerConnectorRealtimeListComponent', () => {
  let component: PartnerConnectorRealtimeListComponent;
  let fixture: ComponentFixture<PartnerConnectorRealtimeListComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PartnerConnectorRealtimeListComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PartnerConnectorRealtimeListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
