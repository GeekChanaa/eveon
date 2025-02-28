import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { AtomsModule } from 'src/app/atoms/atoms.module';
import { SharedModule } from 'src/app/shared/shared.module';
import { RechargeOrdersListComponent } from './recharge-orders-list/recharge-orders-list.component';
import { RechargeOrderComponent } from './recharge-order/recharge-order.component';
import { CreateRechargeOrderComponent } from './create-recharge-order/create-recharge-order.component';
import { RechargeOrdersRoutingModule } from './recharge-orders-routing.module';
@NgModule({
    declarations: [
        RechargeOrdersListComponent,
        RechargeOrderComponent,
        CreateRechargeOrderComponent
    ],
    imports: [
        AtomsModule,
        ReactiveFormsModule,
        CommonModule,
        SharedModule,
        FormsModule,
        RechargeOrdersRoutingModule
    ],
  })
  export class RechargeOrdersModule { }
  