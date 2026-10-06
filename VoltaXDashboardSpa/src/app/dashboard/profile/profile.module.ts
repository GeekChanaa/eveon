import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { AtomsModule } from 'src/app/atoms/atoms.module';
import { SharedModule } from 'src/app/shared/shared.module';
import { ProfileRoutingModule } from './profile-routing.module';
import { AccountInformationsComponent } from './account-informations/account-informations.component';
import { DebitCardsComponent } from './debit-cards/debit-cards.component';
import { NotificationSettingsComponent } from './notification-settings/notification-settings.component';
import { ProfileSecurityComponent } from './profile-security/profile-security.component';
import { RechargeCardsComponent } from './recharge-cards/recharge-cards.component';
import { ProfileComponent } from './profile.component';
import { ProfileUpdateEmailComponent } from './account-informations/profile-update-email/profile-update-email.component';
import { ProfileUpdatePhoneComponent } from './account-informations/profile-update-phone/profile-update-phone.component';
import { ProfileVerifyEmailComponent } from './account-informations/profile-verify-email/profile-verify-email.component';
import { ProfileVerifyPhoneComponent } from './account-informations/profile-verify-phone/profile-verify-phone.component';
import { ProfileSecurityChangePasswordComponent } from './profile-security/profile-security-change-password/profile-security-change-password.component';
import { TwoFactorSettingsModule } from './profile-security/two-factor-settings/two-factor-settings.module';
import { AddDebitCardProfileComponent } from './debit-cards/add-debit-card-profile/add-debit-card-profile.component';
import { ProfilePrivacyComponent } from './profile-privacy/profile-privacy.component';
@NgModule({
    declarations: [
        AccountInformationsComponent,
        DebitCardsComponent,
        NotificationSettingsComponent,
        ProfileSecurityComponent,
        RechargeCardsComponent,
        AccountInformationsComponent,
        ProfileComponent,
        ProfileUpdateEmailComponent, 
        ProfileUpdatePhoneComponent,
        ProfileVerifyEmailComponent,
        ProfileVerifyPhoneComponent,
        ProfileSecurityChangePasswordComponent,
        AddDebitCardProfileComponent,
        ProfilePrivacyComponent
    ],
    imports: [
        AtomsModule,
        ReactiveFormsModule,
        CommonModule,
        SharedModule,
        FormsModule,
        ProfileRoutingModule,
        TwoFactorSettingsModule
    ],
  })
  export class ProfileModule { }
  