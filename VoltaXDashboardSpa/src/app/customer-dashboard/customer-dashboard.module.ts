import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { NgApexchartsModule } from 'ng-apexcharts';
import { AtomsModule } from '../atoms/atoms.module';
import { SharedModule } from '../shared/shared.module';
import { CommonModule } from '@angular/common';
import { CustomerDashboardRoutingModule } from './customer-dashboard-routing.module';
import { CustomerHomeComponent } from './customer-home/customer-home.component';
import { MyCardsComponent } from './my-cards/my-cards.component';
import { SlickCarouselModule } from 'ngx-slick-carousel';
@NgModule({
    declarations: [
        CustomerHomeComponent,
        MyCardsComponent
    ],
    imports: [
        AtomsModule,
        NgApexchartsModule,
        ReactiveFormsModule,
        CommonModule,
        SharedModule,
        FormsModule,
        CustomerDashboardRoutingModule,
        SlickCarouselModule
    ],
  })
  export class CustomerDashboardModule { }
  