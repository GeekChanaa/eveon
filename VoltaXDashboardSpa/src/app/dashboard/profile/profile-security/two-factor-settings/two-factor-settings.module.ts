import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { VxI18nModule } from 'src/app/shared/i18n/vx-i18n.module';
import { TwoFactorSettingsComponent } from './two-factor-settings.component';

@NgModule({
  declarations: [TwoFactorSettingsComponent],
  imports: [CommonModule, FormsModule, VxI18nModule],
  exports: [TwoFactorSettingsComponent]
})
export class TwoFactorSettingsModule { }
