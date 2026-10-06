import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { PartnerLoginComponent } from './partner-login/partner-login.component';
import { PartnerForgotPasswordComponent } from './partner-forgot-password/partner-forgot-password.component';
import { PartnerGoogleCallbackComponent } from './partner-google-callback/partner-google-callback.component';
import { GuestGuard } from 'src/_guards/guest.guard';
import { PartnerResetPasswordComponent } from './partner-reset-password/partner-reset-password.component';



export const AuthRoutes: Routes= [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path : 'login' , component : PartnerLoginComponent, canActivate: [GuestGuard] },
  { path : 'forgot-password' , component : PartnerForgotPasswordComponent, canActivate: [GuestGuard] },
  { path : 'reset-password' , component : PartnerResetPasswordComponent, canActivate: [GuestGuard] },
  { path : 'google-callback' , component : PartnerGoogleCallbackComponent },
]

@NgModule({
  imports: [RouterModule.forChild(AuthRoutes)],
  exports: [RouterModule],
})
export class PartnerAuthRoutingModule{
}
