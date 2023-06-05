  import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { HomeComponent } from './home/home.component';
import { ChargingStationsComponent } from './charging-stations/charging-stations.component';
import { ChargingPointsComponent } from './charging-points/charging-points.component';
import { ConnectorsComponent } from './connectors/connectors.component';
import { StationLoadBalanceComponent } from './station-load-balance/station-load-balance.component';
import { ChargingCardsComponent } from './charging-cards/charging-cards.component';
import { ChargingStrategiesComponent } from './charging-strategies/charging-strategies.component';
import { CommentManagementComponent } from './comment-management/comment-management.component';
import { HomeChargerBindListComponent } from './home-charger-bind-list/home-charger-bind-list.component';
import { CustomersComponent } from './customers/customers.component';
import { TransactionsComponent } from './transactions/transactions.component';
import { RechargeOrdersComponent } from './recharge-orders/recharge-orders.component';
import { AlarmManagementComponent } from './alarm-management/alarm-management.component';
import { OcppLocalListComponent } from './ocpp-local-list/ocpp-local-list.component';
import { ChargingProfileComponent } from './charging-profile/charging-profile.component';
import { OcppConfigurationComponent } from './ocpp-configuration/ocpp-configuration.component';
import { UsersComponent } from './users/users.component';
import { SharedModule } from './shared/shared.module';
import { AtomsComponent } from './atoms/atoms.component';
import { AtomsModule } from './atoms/atoms.module';
import { TabsStatisticsComponent } from './home/tabs-statistics/tabs-statistics.component';
import { HttpClientModule } from '@angular/common/http';
import { NgApexchartsModule } from 'ng-apexcharts';

@NgModule({
  declarations: [																			
    AppComponent,
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
    TabsStatisticsComponent
   ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    SharedModule,
    HttpClientModule,
    AtomsModule,
    NgApexchartsModule,
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
