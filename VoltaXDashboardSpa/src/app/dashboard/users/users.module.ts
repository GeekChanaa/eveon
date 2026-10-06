import { UserAccessComponent } from './user-access/user-access.component';
import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { AtomsModule } from 'src/app/atoms/atoms.module';
import { SharedModule } from 'src/app/shared/shared.module';
import { UsersRoutingModule } from './users-routing.module';
import { UsersListComponent } from './users-list/users-list.component';
import { UserComponent } from './user/user.component';
import { CreateUserComponent } from './create-user/create-user.component';
import { CalendarModule } from 'primeng/calendar';
import { UserChargingSessionsComponent } from './user-charging-sessions/user-charging-sessions.component';
import { UserActionsComponent } from './user-actions/user-actions.component';
import { UserRechargeCardsComponent } from './user-recharge-cards/user-recharge-cards.component';
import { UserOrdersComponent } from './user-orders/user-orders.component';
@NgModule({
    declarations: [
        UsersListComponent,
        UserComponent,
        CreateUserComponent,
        UserChargingSessionsComponent,
        UserActionsComponent,
        UserRechargeCardsComponent,
        UserOrdersComponent
    ],
    imports: [
        UserAccessComponent,
        AtomsModule,
        ReactiveFormsModule,
        CommonModule,
        SharedModule,
        FormsModule,
        UsersRoutingModule,
        CalendarModule
    ],
  })
  export class UsersModule { }
  