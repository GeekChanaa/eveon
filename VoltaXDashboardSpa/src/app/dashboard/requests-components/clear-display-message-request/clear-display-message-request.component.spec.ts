/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { ClearDisplayMessageRequestComponent } from './clear-display-message-request.component';

describe('ClearDisplayMessageRequestComponent', () => {
  let component: ClearDisplayMessageRequestComponent;
  let fixture: ComponentFixture<ClearDisplayMessageRequestComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ClearDisplayMessageRequestComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ClearDisplayMessageRequestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
