/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { HomeChargerBindListComponent } from './home-charger-bind-list.component';

describe('HomeChargerBindListComponent', () => {
  let component: HomeChargerBindListComponent;
  let fixture: ComponentFixture<HomeChargerBindListComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ HomeChargerBindListComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(HomeChargerBindListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
