/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { ConnectorRealtimeChargingSessionsComponent } from './connector-realtime-charging-sessions.component';

describe('ConnectorRealtimeChargingSessionsComponent', () => {
  let component: ConnectorRealtimeChargingSessionsComponent;
  let fixture: ComponentFixture<ConnectorRealtimeChargingSessionsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ConnectorRealtimeChargingSessionsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ConnectorRealtimeChargingSessionsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
