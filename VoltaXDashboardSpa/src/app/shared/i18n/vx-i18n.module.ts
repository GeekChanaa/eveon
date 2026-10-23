import { NgModule, Pipe, PipeTransform } from '@angular/core';
import { formatDate, formatNumber } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { LanguageService } from 'src/_services/language.service';

/** Date in the current interface language (impure: follows a language switch). */
@Pipe({ name: 'vxDate', pure: false })
export class VxDatePipe implements PipeTransform {
  constructor(private language: LanguageService) {}
  transform(value: string | number | Date | null | undefined, format = 'medium'): string {
    if (value == null || value === '') return '—';
    return formatDate(value, format, this.language.locale);
  }
}

/** Amount with two decimals and the currency (MAD / درهم), in the current language. */
@Pipe({ name: 'vxMoney', pure: false })
export class VxMoneyPipe implements PipeTransform {
  constructor(private language: LanguageService) {}
  transform(value: number | null | undefined, currency = 'MAD'): string {
    if (value == null) return '—';
    const amount = formatNumber(value, this.language.locale, '1.2-2');
    const symbol = currency === 'MAD' ? this.language.t('common.currency.mad') : currency;
    return `${amount} ${symbol}`;
  }
}

/** Number in the current language (digits grouping), e.g. {{ kwh | vxNumber:'1.0-2' }}. */
@Pipe({ name: 'vxNumber', pure: false })
export class VxNumberPipe implements PipeTransform {
  constructor(private language: LanguageService) {}
  transform(value: number | null | undefined, digits = '1.0-2'): string {
    if (value == null) return '—';
    return formatNumber(value, this.language.locale, digits);
  }
}

/** Translations (translate pipe / directive) and the localized formatting pipes, for every feature module. */
@NgModule({
  declarations: [VxDatePipe, VxMoneyPipe, VxNumberPipe],
  imports: [TranslateModule],
  exports: [TranslateModule, VxDatePipe, VxMoneyPipe, VxNumberPipe]
})
export class VxI18nModule {}
