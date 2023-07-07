import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { NgApexchartsModule } from 'ng-apexcharts';
import { BrowserModule } from '@angular/platform-browser';
import { AtomsModule } from '../atoms/atoms.module';
import { SharedModule } from '../shared/shared.module';
import { CommonModule } from '@angular/common';
import { AppTableCustomButtonDirective } from 'src/_directives/table-custom-button.directive';
import { GlobalRoutingModule } from './global-routing.module';
import { RechargeCardOrderComponent } from './recharge-card-order/recharge-card-order.component';


@NgModule({
    declarations: [
      RechargeCardOrderComponent
    ],
    imports: [
        GlobalRoutingModule,
        AtomsModule,
        NgApexchartsModule,
        ReactiveFormsModule,
        CommonModule,
        SharedModule,
        FormsModule
    ],
  })
  export class GlobalModule { }
  