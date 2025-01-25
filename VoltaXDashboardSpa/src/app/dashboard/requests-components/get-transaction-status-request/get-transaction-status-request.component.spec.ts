/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { GetTransactionStatusRequestComponent } from './get-transaction-status-request.component';

describe('GetTransactionStatusRequestComponent', () => {
  let component: GetTransactionStatusRequestComponent;
  let fixture: ComponentFixture<GetTransactionStatusRequestComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ GetTransactionStatusRequestComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(GetTransactionStatusRequestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
