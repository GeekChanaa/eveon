/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { SetMonitoringLevelRequestComponent } from './set-monitoring-level-request.component';

describe('SetMonitoringLevelRequestComponent', () => {
  let component: SetMonitoringLevelRequestComponent;
  let fixture: ComponentFixture<SetMonitoringLevelRequestComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ SetMonitoringLevelRequestComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SetMonitoringLevelRequestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
