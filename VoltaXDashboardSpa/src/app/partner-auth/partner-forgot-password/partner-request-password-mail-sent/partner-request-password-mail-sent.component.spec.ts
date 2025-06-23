/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { PartnerRequestPasswordMailSentComponent } from './partner-request-password-mail-sent.component';

describe('PartnerRequestPasswordMailSentComponent', () => {
  let component: PartnerRequestPasswordMailSentComponent;
  let fixture: ComponentFixture<PartnerRequestPasswordMailSentComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PartnerRequestPasswordMailSentComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PartnerRequestPasswordMailSentComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
