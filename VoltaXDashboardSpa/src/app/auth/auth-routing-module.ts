import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { LoginComponent } from './login/login.component';
import { SignUpComponent } from './sign-up/sign-up.component';
import { RequestPasswordComponent } from './request-password/request-password.component';
import { ResetPasswordComponent } from './reset-password/reset-password.component';
import { VerificationMailSentComponent } from './verification-mail-sent/verification-mail-sent.component';
import { VerifyEmailComponent } from './verify-email/verify-email.component';
import { VerifyPhoneComponent } from './verify-phone/verify-phone.component';
import { EnterPhoneNumberComponent } from './enter-phone-number/enter-phone-number.component';
import { CompleteProfileComponent } from './complete-profile/complete-profile.component';
import { ResetPasswordSuccessComponent } from './reset-password-success/reset-password-success.component';
import { GoogleCallbackComponent } from './google-callback/google-callback.component';



export const AuthRoutes: Routes= [
  { path : 'login' , component : LoginComponent },
  { path : 'register' , component : SignUpComponent },
  { path : 'request-password' , component : RequestPasswordComponent },
  { path : 'reset-password' , component : ResetPasswordComponent },
  { path : 'reset-password-success' , component : ResetPasswordSuccessComponent },
  { path : 'verification-mail-sent' , component : VerificationMailSentComponent },
  { path : 'verify-email' , component : VerifyEmailComponent },
  { path : 'verify-phone' , component : VerifyPhoneComponent },
  { path : 'phone-number' , component : EnterPhoneNumberComponent },
  { path : 'complete-profile' , component : CompleteProfileComponent },
  { path : 'google-callback' , component : GoogleCallbackComponent },
]

@NgModule({
  imports: [RouterModule.forChild(AuthRoutes)],
  exports: [RouterModule],
})
export class AuthRoutingModule{
}