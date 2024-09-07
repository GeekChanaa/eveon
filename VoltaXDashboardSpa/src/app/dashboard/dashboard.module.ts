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
import { ChargingStationComponent } from './charging-stations/charging-station/charging-station.component';
import { RechargeCardsComponent } from './profile/recharge-cards/recharge-cards.component';
import { ChargingStationChargePointComponent } from './charging-stations/charging-station-charge-point/charging-station-charge-point.component';
import { UserComponent } from './users/user/user.component';
import { ChargingPointComponent } from './charging-points/charging-point/charging-point.component';
import { ChargingCardComponent } from './charging-cards/charging-card/charging-card.component';
import { StatisticsComponent } from './statistics/statistics.component';
import { AppTableCustomButtonDirective } from 'src/_directives/table-custom-button.directive';
import { NotificationSettingsComponent } from './profile/notification-settings/notification-settings.component';
import { CompleteProfileComponent } from './complete-profile/complete-profile.component';
import {MatExpansionModule} from '@angular/material/expansion'; 
import { MatButtonModule } from '@angular/material/button';
import { DebitCardsComponent } from './profile/debit-cards/debit-cards.component';
import { ChargePointConnectorsComponent } from './charging-points/charging-point/charge-point-connectors/charge-point-connectors.component';
import { ProfileSecurityComponent } from './profile/profile-security/profile-security.component';
import { ChargingStationChargePointsComponent } from './charging-stations/charging-station-charge-points/charging-station-charge-points.component';

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
    ChargingStationComponent,
    ChargingPointsComponent,
    RechargeCardsComponent,
    ChargingStationChargePointComponent,
    UserComponent,
    ChargingPointComponent,
    ChargingCardComponent,
    StatisticsComponent,
    AppTableCustomButtonDirective,
    NotificationSettingsComponent,
    CompleteProfileComponent,
    DebitCardsComponent,
    ChargePointConnectorsComponent,
    ProfileSecurityComponent,
    ChargingStationChargePointsComponent
    
  ],
    imports: [
        DashboardRoutingModule,
        AtomsModule,
        NgApexchartsModule,
        ReactiveFormsModule,
        CommonModule,
        SharedModule,
        FormsModule,
        MatExpansionModule,
        MatButtonModule
    ],
  })
  export class DashboardModule { }
  