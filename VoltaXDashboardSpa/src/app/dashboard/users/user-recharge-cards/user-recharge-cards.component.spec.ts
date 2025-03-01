/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { UserRechargeCardsComponent } from './user-recharge-cards.component';

describe('UserRechargeCardsComponent', () => {
  let component: UserRechargeCardsComponent;
  let fixture: ComponentFixture<UserRechargeCardsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ UserRechargeCardsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(UserRechargeCardsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
