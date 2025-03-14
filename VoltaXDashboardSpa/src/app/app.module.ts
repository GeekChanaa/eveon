import { APP_INITIALIZER, CUSTOM_ELEMENTS_SCHEMA, NgModule } from '@angular/core';
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
import { MatButtonModule } from '@angular/material/button';
import { CalendarModule } from 'primeng/calendar';
import 'prismjs/prism';
import { GoodByeComponent } from './good-bye/good-bye.component';
import { ValidationMessagesService } from 'src/_services/validation-messages.service';


export function loadMessagesFactory(service: ValidationMessagesService) {
  return () => service.loadMessages().toPromise().then(messages => service.setMessages(messages));
}
@NgModule({
  declarations: [			
      AppComponent,
      CustomerDashboardComponent,
      GlobalComponent,
      PartnerDashboardComponent,
      GoodByeComponent
   ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    SharedModule,
    HttpClientModule,
    BrowserAnimationsModule,
    MatSnackBarModule,
    SlickCarouselModule,
    MatRippleModule,
    MatButtonModule,
    CalendarModule
    
  ],
  providers: [
    {
      provide: HTTP_INTERCEPTORS,
      useClass: TokenInterceptor,
      multi: true
    },
    {
      provide: APP_INITIALIZER,
      useFactory: loadMessagesFactory,
      deps: [ValidationMessagesService],
      multi: true
    }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
