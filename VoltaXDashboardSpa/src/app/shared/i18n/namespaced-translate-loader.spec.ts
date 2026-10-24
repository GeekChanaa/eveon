import { HttpClient } from '@angular/common/http';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { NamespacedTranslateLoader, deepMerge } from './namespaced-translate-loader';

describe('NamespacedTranslateLoader', () => {
  let http: HttpTestingController;
  let loader: NamespacedTranslateLoader;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    http = TestBed.inject(HttpTestingController);
    loader = new NamespacedTranslateLoader(TestBed.inject(HttpClient), '/assets/i18n/', ['alarms', 'billing']);
  });

  afterEach(() => http.verify());

  it('merges the core file with every namespace file of the language', () => {
    let result: Record<string, unknown> | undefined;
    loader.getTranslation('ar').subscribe(t => result = t);

    http.expectOne('/assets/i18n/ar.json').flush({ common: { save: 'حفظ' } });
    http.expectOne('/assets/i18n/alarms/ar.json').flush({ alarms: { title: 'الإنذارات' } });
    http.expectOne('/assets/i18n/billing/ar.json').flush({ common: { refund: 'استرداد' } });

    expect(result).toEqual({ common: { save: 'حفظ', refund: 'استرداد' }, alarms: { title: 'الإنذارات' } });
  });

  it('skips a missing namespace file instead of failing the language', () => {
    let result: Record<string, unknown> | undefined;
    loader.getTranslation('fr').subscribe(t => result = t);

    http.expectOne('/assets/i18n/fr.json').flush({ common: { save: 'Enregistrer' } });
    http.expectOne('/assets/i18n/alarms/fr.json').flush('missing', { status: 404, statusText: 'Not Found' });
    http.expectOne('/assets/i18n/billing/fr.json').flush({ billing: { invoice: 'Facture' } });

    expect(result).toEqual({ common: { save: 'Enregistrer' }, billing: { invoice: 'Facture' } });
  });

  it('lets the later file win on a conflicting key without dropping siblings', () => {
    expect(deepMerge({ a: { b: 1, c: 2 } }, { a: { c: 3 } })).toEqual({ a: { b: 1, c: 3 } });
    expect(deepMerge({ a: 'text' }, { a: { b: 1 } })).toEqual({ a: { b: 1 } });
  });
});
