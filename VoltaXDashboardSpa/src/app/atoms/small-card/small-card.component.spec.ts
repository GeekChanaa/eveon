/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { SmallCardComponent } from './small-card.component';

describe('SmallCardComponent', () => {
  let component: SmallCardComponent;
  let fixture: ComponentFixture<SmallCardComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ SmallCardComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SmallCardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
