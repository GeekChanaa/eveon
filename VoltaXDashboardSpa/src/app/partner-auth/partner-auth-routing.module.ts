import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { PartnerLoginComponent } from './partner-login/partner-login.component';
import { PartnerForgotPasswordComponent } from './partner-forgot-password/partner-forgot-password.component';
import { PartnerGoogleCallbackComponent } from './partner-google-callback/partner-google-callback.component';



export const AuthRoutes: Routes= [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path : 'login' , component : PartnerLoginComponent },
  { path : 'forgot-password' , component : PartnerForgotPasswordComponent },
  { path : 'google-callback' , component : PartnerGoogleCallbackComponent },
]

@NgModule({
  imports: [RouterModule.forChild(AuthRoutes)],
  exports: [RouterModule],
})
export class PartnerAuthRoutingModule{
}