import { TestBed } from '@angular/core/testing';
import { configureVxTest } from 'src/testing/vx-testing.module';
import { LanguageService } from 'src/_services/language.service';
import { VxDatePipe, VxMoneyPipe, VxNumberPipe } from '../i18n/vx-i18n.module';
import { VxPagerComponent, connectorTypeLabel, statusTone } from './portal-ui.module';

describe('Portal UI building blocks', () => {
  it('colours statuses by meaning', () => {
    expect(statusTone('Completed')).toBe('vx-badge--success');
    expect(statusTone('Available')).toBe('vx-badge--success');
    expect(statusTone('Pending')).toBe('vx-badge--warning');
    expect(statusTone('Faulted')).toBe('vx-badge--danger');
    expect(statusTone('Expired')).toBe('vx-badge--danger');
    expect(statusTone(undefined)).toBe('vx-badge--info');
  });

  it('names OCPP connector types the way drivers know them', () => {
    expect(connectorTypeLabel('cCCS2')).toBe('CCS2');
    expect(connectorTypeLabel('cG105')).toBe('CHAdeMO');
    expect(connectorTypeLabel('sType2')).toBe('Type 2 (socket)');
    expect(connectorTypeLabel('SomethingNew')).toBe('SomethingNew');
    expect(connectorTypeLabel(null)).toBe('—');
  });

  describe('VxPagerComponent', () => {
    beforeEach(() => configureVxTest([VxPagerComponent]));

    it('counts pages and only emits pages that exist', () => {
      const pager = TestBed.createComponent(VxPagerComponent).componentInstance;
      pager.total = 41;
      pager.pageSize = 20;
      const emitted: number[] = [];
      pager.pageChange.subscribe(p => emitted.push(p));

      pager.go(0);
      pager.go(3);
      pager.go(4);

      expect(pager.pages).toBe(3);
      expect(emitted).toEqual([3]);
    });
  });

  describe('localized pipes', () => {
    let language: LanguageService;

    beforeEach(async () => {
      await configureVxTest([]);
      language = TestBed.inject(LanguageService);
    });

    it('format money, numbers and dates in the current language', async () => {
      const money = new VxMoneyPipe(language);
      const number = new VxNumberPipe(language);
      const date = new VxDatePipe(language);

      await language.use('en');
      expect(money.transform(1234.5)).toContain('1,234.50');
      expect(number.transform(2.345, '1.0-2')).toBe('2.35');
      expect(date.transform('2026-10-23T10:00:00Z', 'yyyy')).toBe('2026');

      await language.use('fr');
      expect(money.transform(1234.5)).toMatch(/1\s?234,50/);
      expect(number.transform(null)).toBe('—');
    });

    it('mirrors the page for Arabic and remembers the choice', async () => {
      await language.use('ar');

      expect(document.documentElement.dir).toBe('rtl');
      expect(document.documentElement.lang).toBe('ar');
      expect(localStorage.getItem('lang')).toBe('ar');
      expect(language.initial()).toBe('ar');

      await language.use('fr');
      expect(document.documentElement.dir).toBe('ltr');
    });
  });
});
