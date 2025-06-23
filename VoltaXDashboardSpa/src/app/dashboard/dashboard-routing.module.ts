import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AlarmManagementComponent } from './alarm-management/alarm-management.component';
import { ChargingCardsComponent } from './charging-cards/charging-cards.component';
import { ChargingPointsComponent } from './charging-points/charging-points.component';
import { ChargingProfileComponent } from './charging-profile/charging-profile.component';
import { ChargingStationsComponent } from './charging-stations/charging-stations.component';
import { ChargingStrategiesComponent } from './charging-strategies/charging-strategies.component';
import { HomeChargerBindListComponent } from './home-charger-bind-list/home-charger-bind-list.component';
import { HomeComponent } from './home/home.component';
import { OcppConfigurationComponent } from './ocpp-configuration/ocpp-configuration.component';
import { OcppLocalListComponent } from './ocpp-local-list/ocpp-local-list.component';
import { RechargeOrdersComponent } from './recharge-orders/recharge-orders.component';
import { StationLoadBalanceComponent } from './station-load-balance/station-load-balance.component';
import { TransactionsComponent } from './transactions/transactions.component';
import { UsersComponent } from './users/users.component';
import { ProfileComponent } from './profile/profile.component';
import { CreateChargingCardComponent } from './charging-cards/create-charging-card/create-charging-card.component';
import { ChargingCardComponent } from './charging-cards/charging-card/charging-card.component';
import { StatisticsComponent } from './statistics/statistics.component';
import { CommentsComponent } from './comments/comments.component';
import { ReportsComponent } from './reports/reports.component';
import { ConnectorRealtimeComponent } from './connector-realtime/connector-realtime.component';
import { SystemReportsComponent } from './system-reports/system-reports.component';
import { GlobalConfigurationsComponent } from './global-configurations/global-configurations.component';
import { DocumentationComponent } from './documentation/documentation.component';
import { PartnersComponent } from './partners/partners.component';
import { ChargingSessionsComponent } from './charging-sessions/charging-sessions.component';
import { GoodByeComponent } from '../good-bye/good-bye.component';
import { NoticesComponent } from './notices/notices.component';
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
    loadChildren : () => import('./charging-cards/charging-cards.module')
      .then(m=>m.ChargingCardsModule)
  },
  {
    path: "profile",
    component: ProfileComponent,
    loadChildren : () => import('./profile/profile.module')
      .then(m=>m.ProfileModule)
  },
  {
    path: "charging-cards/create",
    component: CreateChargingCardComponent,
  },{
    path: "charging-cards/:id",
    component: ChargingCardComponent,
  },
  {
    path: "charging-points",
    component: ChargingPointsComponent,
    loadChildren : () => import('./charging-points/charging-points.module')
      .then(m=>m.ChargingPointsModule)
  },
  {
    path: "system-reports",
    component: SystemReportsComponent,
    loadChildren : () => import('./system-reports/system-reports.module')
      .then(m=>m.SystemReportsModule)
  },
  {
    path: "charging-profile",
    component: ChargingProfileComponent,
  },
  {
    path: "global-configurations",
    component: GlobalConfigurationsComponent,
  },
  {
    path: "charging-stations",
    component: ChargingStationsComponent,
    loadChildren : () => import('./charging-stations/charging-stations.module')
      .then(m=>m.ChargingStationsModule)
  },
  {
    path: "charging-sessions",
    component: ChargingSessionsComponent,
    loadChildren : () => import('./charging-sessions/charging-sessions.module')
      .then(m=>m.ChargingSessionsModule)
  },
  {
    path: "comments",
    component: CommentsComponent,
    loadChildren : () => import('./comments/comments.module')
      .then(m=>m.CommentsModule)
  },
  {
    path: "reports",
    component: ReportsComponent,
    loadChildren : () => import('./reports/reports.module')
      .then(m=>m.ReportsModule)
  },
  {
    path: "charging-strategies",
    component: ChargingStrategiesComponent,
  },
  {
    path: "connector-realtime",
    component: ConnectorRealtimeComponent,
    loadChildren : () => import('./connector-realtime/connector-realtime.module')
      .then(m=>m.ConnectorRealtimeModule)
  },
  {
    path: "documentation",
    component: DocumentationComponent,
    loadChildren : () => import('./documentation/documentation.module')
      .then(m=>m.DocumentationModule)
  },
  {
    path: "partners",
    component: PartnersComponent,
    loadChildren : () => import('./partners/partners.module')
      .then(m=>m.PartnersModule)
  },
  {
    path: "users",
    component: UsersComponent,
    loadChildren : () => import('./users/users.module')
      .then(m=>m.UsersModule)
  },
  {
    path: "notices",
    component: NoticesComponent,
    loadChildren : () => import('./notices/notices.module')
      .then(m=>m.NoticesModule)
  },
  {
    path: "recharge-orders",
    component: RechargeOrdersComponent,
    loadChildren : () => import('./recharge-orders/recharge-orders.module')
      .then(m=>m.RechargeOrdersModule)
  },
  {
    path: "user-info-download-requests",
    component: RechargeOrdersComponent,
    loadChildren : () => import('./user-info-download-requests/user-info-download-requests.module')
      .then(m=>m.UserInfoDownloadRequestsModule)
  },
  {
    path: "permissions",
    component: RechargeOrdersComponent,
    loadChildren : () => import('./permissions/permissions.module')
      .then(m=>m.PermissionsModule)
  },
  {
    path: "roles",
    component: RechargeOrdersComponent,
    loadChildren : () => import('./roles/roles.module')
      .then(m=>m.RolesModule)
  },
  {
    path: "statistics",
    component: StatisticsComponent,
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
    loadChildren : () => import('./ocpp-local-list/ocpp-local-list.module')
      .then(m=>m.OcppLocalListModule)
  },
  {
    path: "station-load-balance",
    component: StationLoadBalanceComponent,
  },
  {
    path: "transactions",
    component: TransactionsComponent,
    loadChildren : () => import('./transactions/transactions.module')
      .then(m=>m.TransactionsModule)
  },
  
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class DashboardRoutingModule { }
