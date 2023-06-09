import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { AlarmManagementComponent } from './alarm-management/alarm-management.component';
import { ChargingCardsComponent } from './charging-cards/charging-cards.component';
import { ChargingPointsComponent } from './charging-points/charging-points.component';
import { ChargingProfileComponent } from './charging-profile/charging-profile.component';
import { ChargingStationsComponent } from './charging-stations/charging-stations.component';
import { ChargingStrategiesComponent } from './charging-strategies/charging-strategies.component';
import { CommentManagementComponent } from './comment-management/comment-management.component';
import { ConnectorsComponent } from './connectors/connectors.component';
import { CustomersComponent } from './customers/customers.component';
import { HomeChargerBindListComponent } from './home-charger-bind-list/home-charger-bind-list.component';
import { OcppConfigurationComponent } from './ocpp-configuration/ocpp-configuration.component';
import { OcppLocalListComponent } from './ocpp-local-list/ocpp-local-list.component';
import { RechargeOrdersComponent } from './recharge-orders/recharge-orders.component';
import { StationLoadBalanceComponent } from './station-load-balance/station-load-balance.component';
import { TransactionsComponent } from './transactions/transactions.component';
import { UsersComponent } from './users/users.component';
import { CreateChargingStationComponent } from './charging-stations/create-charging-station/create-charging-station.component';

const routes: Routes = [
  {
    path: "",
    component: HomeComponent,
  },
  {
    path: "alarm-management",
    component: AlarmManagementComponent,
  },
  {
    path: "charging-cards",
    component: ChargingCardsComponent,
  },
  {
    path: "charging-points",
    component: ChargingPointsComponent,
  },
  {
    path: "create-charging-station",
    component: CreateChargingStationComponent,
  },
  {
    path: "charging-profile",
    component: ChargingProfileComponent,
  },
  {
    path: "charging-stations",
    component: ChargingStationsComponent,
  },
  {
    path: "charging-strategies",
    component: ChargingStrategiesComponent,
  },
  {
    path: "comment-management",
    component: CommentManagementComponent,
  },
  {
    path: "connectors",
    component: ConnectorsComponent,
  },
  {
    path: "customers",
    component: CustomersComponent,
  },
  {
    path: "home-charger-bind-list",
    component: HomeChargerBindListComponent,
  },
  {
    path: "ocpp-configuration",
    component: OcppConfigurationComponent,
  },
  {
    path: "ocpp-local-list",
    component: OcppLocalListComponent,
  },
  {
    path: "recharge-orders",
    component: RechargeOrdersComponent,
  },
  {
    path: "station-load-balance",
    component: StationLoadBalanceComponent,
  },
  {
    path: "transactions",
    component: TransactionsComponent,
  },
  {
    path: "users",
    component: UsersComponent,
  },
  
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
