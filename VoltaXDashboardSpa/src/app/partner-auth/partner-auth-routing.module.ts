import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { PartnerLoginComponent } from './partner-login/partner-login.component';
import { PartnerForgotPasswordComponent } from './partner-forgot-password/partner-forgot-password.component';



export const AuthRoutes: Routes= [
  { path : 'login' , component : PartnerLoginComponent },
  { path : 'forgot-password' , component : PartnerForgotPasswordComponent },
]

@NgModule({
  imports: [RouterModule.forChild(AuthRoutes)],
  exports: [RouterModule],
})
export class PartnerAuthRoutingModule{
}