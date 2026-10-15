import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthRoutingModule } from './auth-routing-module';
import { AtomsModule } from '../atoms/atoms.module';
import { AuthComponent } from './auth.component';
import { GoogleCallbackComponent } from './google-callback/google-callback.component';
import { SessionComponent } from './session/session.component';



@NgModule({
    declarations: [
        AuthComponent,
        GoogleCallbackComponent,
        SessionComponent
  ],
    imports: [
      CommonModule,
      AuthRoutingModule,
      AtomsModule
    ],
  })
  export class AuthModule { }
