import { CUSTOM_ELEMENTS_SCHEMA, NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { SharedModule } from './shared/shared.module';
import { HttpClientModule } from '@angular/common/http';
import { AuthComponent } from './auth/auth.component';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { AppTableCustomButtonDirective } from 'src/_directives/table-custom-button.directive';
import { CustomerDashboardComponent } from './customer-dashboard/customer-dashboard.component';
import { SlickCarouselModule } from 'ngx-slick-carousel';

@NgModule({
  declarations: [	
    AppComponent,
    AuthComponent,
      CustomerDashboardComponent
   ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    SharedModule,
    HttpClientModule,
    BrowserAnimationsModule,
    MatSnackBarModule,
    SlickCarouselModule
    
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
