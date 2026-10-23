/* tslint:disable:no-unused-variable */
import { waitForAsync, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { UserRechargeCardsComponent } from './user-recharge-cards.component';

import { VX_TEST_SCHEMAS, VxTestingModule } from 'src/testing/vx-testing.module';
describe('UserRechargeCardsComponent', () => {
  let component: UserRechargeCardsComponent;
  let fixture: ComponentFixture<UserRechargeCardsComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      schemas: VX_TEST_SCHEMAS,
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
