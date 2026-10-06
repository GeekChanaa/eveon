import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TwoFactorSettingsComponent } from './two-factor-settings.component';

@NgModule({
  declarations: [TwoFactorSettingsComponent],
  imports: [CommonModule, FormsModule],
  exports: [TwoFactorSettingsComponent]
})
export class TwoFactorSettingsModule { }
