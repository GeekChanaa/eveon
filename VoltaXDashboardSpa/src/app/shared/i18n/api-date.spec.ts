import { TestBed } from '@angular/core/testing';
import { LanguageService } from 'src/_services/language.service';
import { configureVxTest } from 'src/testing/vx-testing.module';
import { VxDatePipe, apiDate } from './vx-i18n.module';

/** The API stores UTC and writes it without offset: the dashboard must not read those times as local ones. */
describe('API times', () => {
  it('reads times without offset as UTC instants and leaves the others alone', () => {
    expect(apiDate('2026-10-20T09:00:00').toISOString()).toBe('2026-10-20T09:00:00.000Z');
    expect(apiDate('2026-10-20T09:00:00.25').toISOString()).toBe('2026-10-20T09:00:00.250Z');
    expect(apiDate('2026-10-20T09:00:00+01:00').toISOString()).toBe('2026-10-20T08:00:00.000Z');
    expect(apiDate('2026-10-20T09:00:00Z').toISOString()).toBe('2026-10-20T09:00:00.000Z');
  });

  it('shows them in the browser\'s time zone', async () => {
    await configureVxTest([]);
    const pipe = new VxDatePipe(TestBed.inject(LanguageService));
    const utc = new Date('2026-10-20T09:00:00Z');
    const local = `${String(utc.getHours()).padStart(2, '0')}:${String(utc.getMinutes()).padStart(2, '0')}`;

    expect(pipe.transform('2026-10-20T09:00:00', 'HH:mm')).toBe(local);
    expect(pipe.transform('2026-10-24', 'yyyy-MM-dd')).toBe('2026-10-24'); // a date alone stays that date
    expect(pipe.transform(null)).toBe('—');
  });
});
