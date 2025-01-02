/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { StoppedReasonTagComponent } from './stopped-reason-tag.component';

describe('StoppedReasonTagComponent', () => {
  let component: StoppedReasonTagComponent;
  let fixture: ComponentFixture<StoppedReasonTagComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ StoppedReasonTagComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(StoppedReasonTagComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
