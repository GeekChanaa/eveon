/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { SetMonitoringBaseRequestComponent } from './set-monitoring-base-request.component';

describe('SetMonitoringBaseRequestComponent', () => {
  let component: SetMonitoringBaseRequestComponent;
  let fixture: ComponentFixture<SetMonitoringBaseRequestComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ SetMonitoringBaseRequestComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SetMonitoringBaseRequestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
