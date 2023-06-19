import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { LoginComponent } from './login/login.component';
import { SignUpComponent } from './sign-up/sign-up.component';
import { RequestPasswordComponent } from './request-password/request-password.component';
import { ResetPasswordComponent } from './reset-password/reset-password.component';



export const AuthRoutes: Routes= [
  { path : 'login' , component : LoginComponent },
  { path : 'register' , component : SignUpComponent },
  { path : 'requestpassword' , component : RequestPasswordComponent },
  { path : 'resetpassword' , component : ResetPasswordComponent },
]

@NgModule({
  imports: [RouterModule.forChild(AuthRoutes)],
  exports: [RouterModule],
})
export class AuthRoutingModule{
}