import { CUSTOM_ELEMENTS_SCHEMA, NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { SharedModule } from './shared/shared.module';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { AuthComponent } from './auth/auth.component';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { AppTableCustomButtonDirective } from 'src/_directives/table-custom-button.directive';
import { CustomerDashboardComponent } from './customer-dashboard/customer-dashboard.component';
import { SlickCarouselModule } from 'ngx-slick-carousel';
import { MatRippleModule } from '@angular/material/core';
import { GlobalComponent } from './global/global.component';
import { TokenInterceptor } from './auth/token.interceptor';
import { PartnerDashboardComponent } from './partner-dashboard/partner-dashboard.component';

@NgModule({
  declarations: [			
    AppComponent,
    AuthComponent,
      CustomerDashboardComponent,
      GlobalComponent,
      PartnerDashboardComponent
   ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    SharedModule,
    HttpClientModule,
    BrowserAnimationsModule,
    MatSnackBarModule,
    SlickCarouselModule,
    MatRippleModule
    
  ],
  providers: [
    {
      provide: HTTP_INTERCEPTORS,
      useClass: TokenInterceptor,
      multi: true
    },
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
