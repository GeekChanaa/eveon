/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { ConnectorRealtimeOverviewStatisticsComponent } from './connector-realtime-overview-statistics.component';

describe('ConnectorRealtimeOverviewStatisticsComponent', () => {
  let component: ConnectorRealtimeOverviewStatisticsComponent;
  let fixture: ComponentFixture<ConnectorRealtimeOverviewStatisticsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ConnectorRealtimeOverviewStatisticsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ConnectorRealtimeOverviewStatisticsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
