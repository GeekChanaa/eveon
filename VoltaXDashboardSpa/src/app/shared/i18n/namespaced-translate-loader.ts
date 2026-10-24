import { HttpClient } from '@angular/common/http';
import { TranslateLoader } from '@ngx-translate/core';
import { Observable, catchError, forkJoin, map, of } from 'rxjs';

/**
 * Feature translation files, next to the core one: src/assets/i18n/{namespace}/{lang}.json.
 * Each file holds its keys under its own top-level object (e.g. { "alarms": { ... } }) so features
 * never edit the same file. Add a namespace here when a feature gets its own file.
 */
export const I18N_NAMESPACES = ['auth', 'operator', 'alarms', 'billing', 'portal', 'charging'] as const;

type Translations = Record<string, unknown>;

const isObject = (value: unknown): value is Translations =>
  value != null && typeof value === 'object' && !Array.isArray(value);

/** Merges b into a, object by object; b wins on conflicting leaves. */
export function deepMerge(a: Translations, b: Translations): Translations {
  const result: Translations = { ...a };
  for (const [key, value] of Object.entries(b)) {
    result[key] = isObject(result[key]) && isObject(value) ? deepMerge(result[key] as Translations, value) : value;
  }
  return result;
}

/**
 * Loads src/assets/i18n/{lang}.json and every namespace file for that language, merged into one table.
 * A missing or broken namespace file is skipped, so a feature without translations yet never blocks the app.
 */
export class NamespacedTranslateLoader implements TranslateLoader {
  constructor(private http: HttpClient, private prefix = '/assets/i18n/', private namespaces: readonly string[] = I18N_NAMESPACES) {}

  getTranslation(lang: string): Observable<Translations> {
    const file = (path: string) => this.http.get<Translations>(`${this.prefix}${path}.json`).pipe(catchError(() => of({})));
    return forkJoin([file(lang), ...this.namespaces.map(ns => file(`${ns}/${lang}`))]).pipe(
      map(tables => tables.reduce((merged, table) => deepMerge(merged, isObject(table) ? table : {}), {} as Translations))
    );
  }
}
