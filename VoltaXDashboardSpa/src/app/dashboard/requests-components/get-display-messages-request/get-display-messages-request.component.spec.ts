/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { GetDisplayMessagesRequestComponent } from './get-display-messages-request.component';

describe('GetDisplayMessagesRequestComponent', () => {
  let component: GetDisplayMessagesRequestComponent;
  let fixture: ComponentFixture<GetDisplayMessagesRequestComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ GetDisplayMessagesRequestComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(GetDisplayMessagesRequestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
