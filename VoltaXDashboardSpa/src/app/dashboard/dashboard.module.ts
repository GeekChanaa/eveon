import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { DashboardRoutingModule } from './dashboard-routing.module';
import { AlarmManagementComponent } from './alarm-management/alarm-management.component';
import { ChargingCardsComponent } from './charging-cards/charging-cards.component';
import { ChargingPointsComponent } from './charging-points/charging-points.component';
import { ChargingProfileComponent } from './charging-profile/charging-profile.component';
import { ChargingStationsComponent } from './charging-stations/charging-stations.component';
import { CreateChargingStationComponent } from './charging-stations/create-charging-station/create-charging-station.component';
import { ChargingStrategiesComponent } from './charging-strategies/charging-strategies.component';
import { CommentManagementComponent } from './comment-management/comment-management.component';
import { ConnectorsComponent } from './connectors/connectors.component';
import { CustomersComponent } from './customers/customers.component';
import { HomeChargerBindListComponent } from './home-charger-bind-list/home-charger-bind-list.component';
import { HomeComponent } from './home/home.component';
import { TabsStatisticsComponent } from './home/tabs-statistics/tabs-statistics.component';
import { OcppConfigurationComponent } from './ocpp-configuration/ocpp-configuration.component';
import { OcppLocalListComponent } from './ocpp-local-list/ocpp-local-list.component';
import { RechargeOrdersComponent } from './recharge-orders/recharge-orders.component';
import { StationLoadBalanceComponent } from './station-load-balance/station-load-balance.component';
import { TransactionsComponent } from './transactions/transactions.component';
import { UsersComponent } from './users/users.component';
import { DashboardComponent } from './dashboard.component';
import { NgApexchartsModule } from 'ng-apexcharts';
import { BrowserModule } from '@angular/platform-browser';
import { AtomsModule } from '../atoms/atoms.module';
import { SharedModule } from '../shared/shared.module';
import { CommonModule } from '@angular/common';
import { ProfileComponent } from './profile/profile.component';
import { CreateChargingCardComponent } from './charging-cards/create-charging-card/create-charging-card.component';
import { DebitCardComponent } from './profile/debit-card/debit-card.component';
import { ChargingStationComponent } from './charging-stations/charging-station/charging-station.component';
import { RechargeCardsComponent } from './profile/recharge-cards/recharge-cards.component';
import { ChargingStationChargePointComponent } from './charging-stations/charging-station-charge-point/charging-station-charge-point.component';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { UserComponent } from './users/user/user.component';
import { ChargingPointComponent } from './charging-points/charging-point/charging-point.component';
import { ChargingCardComponent } from './charging-cards/charging-card/charging-card.component';


@NgModule({
    declarations: [
        HomeComponent,
    ChargingStationsComponent,
    ChargingPointsComponent,
    ConnectorsComponent,
    StationLoadBalanceComponent,
    ChargingCardsComponent,
    ChargingStrategiesComponent,
    CommentManagementComponent,
    HomeChargerBindListComponent,
    CustomersComponent,
    TransactionsComponent,
    RechargeOrdersComponent,
    AlarmManagementComponent,
    OcppLocalListComponent,
    ChargingProfileComponent,
    OcppConfigurationComponent,
    UsersComponent,
    TabsStatisticsComponent,
    CreateChargingStationComponent,
    DashboardComponent,
    ProfileComponent,
    CreateChargingCardComponent,
    DebitCardComponent,
    ChargingStationComponent,
    ChargingPointsComponent,
    RechargeCardsComponent,
    ChargingStationChargePointComponent,
    UserComponent,
    ChargingPointComponent,
    ChargingCardComponent
    
  ],
    imports: [
        DashboardRoutingModule,
        AtomsModule,
        NgApexchartsModule,
        ReactiveFormsModule,
        CommonModule,
        SharedModule,
        FormsModule
    ],
  })
  export class DashboardModule { }
  