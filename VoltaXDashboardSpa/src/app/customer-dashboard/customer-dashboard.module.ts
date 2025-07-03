import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { NgApexchartsModule } from 'ng-apexcharts';
import { AtomsModule } from '../atoms/atoms.module';
import { SharedModule } from '../shared/shared.module';
import { CommonModule } from '@angular/common';
import { CustomerDashboardRoutingModule } from './customer-dashboard-routing.module';
import { CustomerHomeComponent } from './customer-home/customer-home.component';
import { SlickCarouselModule } from 'ngx-slick-carousel';
import { MatRippleModule } from '@angular/material/core';
import {MatTabsModule} from '@angular/material/tabs'; 
import { CustomerHomeEmailVerificationComponent } from './customer-home/customer-home-email-verification/customer-home-email-verification.component';
import { CustomerHomePhoneVerificationComponent } from './customer-home/customer-home-phone-verification/customer-home-phone-verification.component';
@NgModule({
    declarations: [
        CustomerHomeComponent,
        CustomerHomeEmailVerificationComponent,
        CustomerHomePhoneVerificationComponent
    ],
    imports: [
        AtomsModule,
        NgApexchartsModule,
        ReactiveFormsModule,
        CommonModule,
        SharedModule,
        FormsModule,
        CustomerDashboardRoutingModule,
        SlickCarouselModule,
        MatRippleModule,
        MatTabsModule
    ],
  })
  export class CustomerDashboardModule { }
  