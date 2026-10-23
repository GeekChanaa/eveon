import { DOCUMENT, registerLocaleData } from '@angular/common';
import { Inject, Injectable } from '@angular/core';
import localeFr from '@angular/common/locales/fr';
import localeArMa from '@angular/common/locales/ar-MA';
import { TranslateService } from '@ngx-translate/core';
import { BehaviorSubject, Observable, firstValueFrom } from 'rxjs';

export type LanguageCode = 'fr' | 'en' | 'ar';

export interface Language {
  code: LanguageCode;
  /** Name in the language itself, as shown in the switcher. */
  label: string;
  dir: 'ltr' | 'rtl';
  /** Angular locale used by the date / number pipes. */
  locale: string;
}

export const LANGUAGES: Language[] = [
  { code: 'fr', label: 'Français', dir: 'ltr', locale: 'fr' },
  { code: 'en', label: 'English', dir: 'ltr', locale: 'en-US' },
  { code: 'ar', label: 'العربية', dir: 'rtl', locale: 'ar-MA' }
];

const STORAGE_KEY = 'lang';

registerLocaleData(localeFr, 'fr');
registerLocaleData(localeArMa, 'ar-MA');

/**
 * The interface language: French (default, Morocco), English or Arabic (right to left). The choice is kept in
 * localStorage and applied to <html lang dir> so the layout mirrors for Arabic.
 */
@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly current$ = new BehaviorSubject<Language>(LANGUAGES[0]);
  readonly languages = LANGUAGES;
  readonly changes: Observable<Language> = this.current$.asObservable();

  constructor(private translate: TranslateService, @Inject(DOCUMENT) private document: Document) {
    translate.addLangs(LANGUAGES.map(l => l.code));
    translate.setDefaultLang('fr');
  }

  get current(): Language { return this.current$.value; }
  get locale(): string { return this.current.locale; }
  get isRtl(): boolean { return this.current.dir === 'rtl'; }

  /** Language at start: the saved choice, else the browser's, else French. */
  initial(): LanguageCode {
    const saved = this.read();
    if (saved) return saved;
    const browser = (navigator.language || '').slice(0, 2).toLowerCase();
    return (LANGUAGES.find(l => l.code === browser)?.code) ?? 'fr';
  }

  /** Switches the language; resolves once its translations are loaded. */
  async use(code: LanguageCode | string): Promise<void> {
    const language = LANGUAGES.find(l => l.code === code) ?? LANGUAGES[0];
    await firstValueFrom(this.translate.use(language.code));
    const html = this.document.documentElement;
    html.lang = language.code;
    html.dir = language.dir;
    this.document.body?.classList.toggle('rtl', language.dir === 'rtl');
    try { localStorage.setItem(STORAGE_KEY, language.code); } catch { /* private mode */ }
    this.current$.next(language);
  }

  /** Instant translation for code (toasts, confirmations); the template pipe is preferred. */
  t(key: string, params?: Record<string, unknown>): string {
    return this.translate.instant(key, params);
  }

  private read(): LanguageCode | null {
    try {
      const value = localStorage.getItem(STORAGE_KEY);
      return LANGUAGES.some(l => l.code === value) ? value as LanguageCode : null;
    } catch {
      return null;
    }
  }
}

/** APP_INITIALIZER: load the language before the first render so no key is ever shown. */
export function initLanguageFactory(language: LanguageService) {
  return () => language.use(language.initial()).catch(() => undefined);
}
