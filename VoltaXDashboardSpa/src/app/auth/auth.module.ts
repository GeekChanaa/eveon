import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthRoutingModule } from './auth-routing-module';
import { AtomsModule } from '../atoms/atoms.module';
import { VxI18nModule } from '../shared/i18n/vx-i18n.module';
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
      AtomsModule,
      VxI18nModule
    ],
  })
  export class AuthModule { }
