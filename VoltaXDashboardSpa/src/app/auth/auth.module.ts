import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { AuthRoutingModule } from './auth-routing-module';
import { LoginComponent } from './login/login.component';
import { RequestPasswordComponent } from './request-password/request-password.component';
import { ResetPasswordComponent } from './reset-password/reset-password.component';
import { SignUpComponent } from './sign-up/sign-up.component';



@NgModule({
    declarations: [
        LoginComponent,
        SignUpComponent,
        RequestPasswordComponent,
        ResetPasswordComponent
  ],
    imports: [
      CommonModule,
      ReactiveFormsModule ,
      HttpClientModule,
      FormsModule,
      AuthRoutingModule
    ],
  })
  export class AuthModule { }
  