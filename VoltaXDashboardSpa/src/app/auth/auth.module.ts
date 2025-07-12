import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { AuthRoutingModule } from './auth-routing-module';
import { LoginComponent } from './login/login.component';
import { RequestPasswordComponent } from './request-password/request-password.component';
import { ResetPasswordComponent } from './reset-password/reset-password.component';
import { SignUpComponent } from './sign-up/sign-up.component';
import { VerifyEmailComponent } from './verify-email/verify-email.component';
import { VerifyPhoneComponent } from './verify-phone/verify-phone.component';
import { VerificationMailSentComponent } from './verification-mail-sent/verification-mail-sent.component';
import { EnterPhoneNumberComponent } from './enter-phone-number/enter-phone-number.component';
import { CompleteProfileComponent } from './complete-profile/complete-profile.component';
import { AtomsModule } from '../atoms/atoms.module';
import { AuthComponent } from './auth.component';
import { RequestPasswordMailSentComponent } from './request-password/request-password-mail-sent/request-password-mail-sent.component';



@NgModule({
    declarations: [
        LoginComponent,
        SignUpComponent,
        RequestPasswordComponent,
        ResetPasswordComponent,
        VerifyEmailComponent,
        VerifyPhoneComponent,
        VerificationMailSentComponent,
        EnterPhoneNumberComponent,
        CompleteProfileComponent,
        AuthComponent,
        RequestPasswordMailSentComponent,
  ],
    imports: [
      CommonModule,
      ReactiveFormsModule ,
      HttpClientModule,
      FormsModule,
      AuthRoutingModule,
      AtomsModule
    ],
  })
  export class AuthModule { }
  