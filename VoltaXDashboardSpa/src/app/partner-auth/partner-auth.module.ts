import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { PartnerAuthRoutingModule } from './partner-auth-routing.module';
import { AtomsModule } from '../atoms/atoms.module';
import { PartnerLoginComponent } from './partner-login/partner-login.component';
import { PartnerForgotPasswordComponent } from './partner-forgot-password/partner-forgot-password.component';
import { PartnerAuthComponent } from './partner-auth.component';
import { PartnerRequestPasswordMailSentComponent } from './partner-forgot-password/partner-request-password-mail-sent/partner-request-password-mail-sent.component';



@NgModule({
    declarations: [
        PartnerLoginComponent,
        PartnerForgotPasswordComponent,
        PartnerAuthComponent,
        PartnerRequestPasswordMailSentComponent
  ],
    imports: [
      CommonModule,
      ReactiveFormsModule ,
      HttpClientModule,
      FormsModule,
      PartnerAuthRoutingModule,
      AtomsModule
    ],
  })
  export class PartnerAuthModule { }
  