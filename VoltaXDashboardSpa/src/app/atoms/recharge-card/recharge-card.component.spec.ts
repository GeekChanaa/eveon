/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { RechargeCardComponent } from './recharge-card.component';

describe('RechargeCardComponent', () => {
  let component: RechargeCardComponent;
  let fixture: ComponentFixture<RechargeCardComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ RechargeCardComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(RechargeCardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('hides the card number and balance by default', () => {
    const card = fixture.nativeElement as HTMLElement;

    expect(card.textContent).not.toContain('1234 5678 9012 3456');
    expect(card.textContent).not.toContain('85.50 MAD');
    expect(card.querySelector('[aria-label="Show card number"]')).toBeTruthy();
    expect(card.querySelector('[aria-label="Show balance"]')).toBeTruthy();
  });

  it('reveals sensitive details only when their eye buttons are clicked', () => {
    const card = fixture.nativeElement as HTMLElement;
    const cardNumberButton = card.querySelector('[aria-label="Show card number"]') as HTMLButtonElement;
    const balanceButton = card.querySelector('[aria-label="Show balance"]') as HTMLButtonElement;

    cardNumberButton.click();
    fixture.detectChanges();
    expect(card.textContent).toContain('1234 5678 9012 3456');
    expect(card.textContent).not.toContain('85.50 MAD');

    balanceButton.click();
    fixture.detectChanges();
    expect(card.textContent).toContain('85.50 MAD');
  });
});
