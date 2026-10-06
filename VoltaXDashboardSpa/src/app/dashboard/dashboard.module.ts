import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { DashboardRoutingModule } from './dashboard-routing.module';
import { AlarmManagementComponent } from './alarm-management/alarm-management.component';
import { ChargingProfileComponent } from './charging-profile/charging-profile.component';
import { ChargingStrategiesComponent } from './charging-strategies/charging-strategies.component';
import { HomeChargerBindListComponent } from './home-charger-bind-list/home-charger-bind-list.component';
import { HomeComponent } from './home/home.component';
import { TabsStatisticsComponent } from './home/tabs-statistics/tabs-statistics.component';
import { OcppConfigurationComponent } from './ocpp-configuration/ocpp-configuration.component';
import { ChargePointProvisioningComponent } from './charge-point-provisioning/charge-point-provisioning.component';
import { RechargeOrdersComponent } from './recharge-orders/recharge-orders.component';
import { StationLoadBalanceComponent } from './station-load-balance/station-load-balance.component';
import { UsersComponent } from './users/users.component';
import { DashboardComponent } from './dashboard.component';
import { NgApexchartsModule } from 'ng-apexcharts';
import { AtomsModule } from '../atoms/atoms.module';
import { SharedModule } from '../shared/shared.module';
import { CommonModule } from '@angular/common';
import { StatisticsComponent } from './statistics/statistics.component';
import { CompleteProfileComponent } from './complete-profile/complete-profile.component';
import {MatExpansionModule} from '@angular/material/expansion'; 
import { MatButtonModule } from '@angular/material/button';
import { GlobalConfigurationsComponent } from './global-configurations/global-configurations.component';
import { ConnectorStatusesComponent } from './home/connector-statuses/connector-statuses.component';
import { PartnersComponent } from './partners/partners.component';
import { ChargingSessionsComponent } from './charging-sessions/charging-sessions.component';

@NgModule({
    declarations: [
      HomeComponent,
      StationLoadBalanceComponent,
      ChargingStrategiesComponent,
      HomeChargerBindListComponent,
      RechargeOrdersComponent,
      AlarmManagementComponent,
      ChargingProfileComponent,
      OcppConfigurationComponent,
      ChargePointProvisioningComponent,
      TabsStatisticsComponent,
      DashboardComponent,
      StatisticsComponent,
      CompleteProfileComponent,
      GlobalConfigurationsComponent,
      ConnectorStatusesComponent,
      PartnersComponent,
      UsersComponent,
      ChargingSessionsComponent,
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
        MatButtonModule,
        
    ],
  })
  export class DashboardModule { }
  