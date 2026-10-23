import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { TwoFactorSettingsComponent } from '../dashboard/profile/profile-security/two-factor-settings/two-factor-settings.component';
import { PartnerOverviewComponent } from './portal/partner-overview.component';
import { PartnerStationsComponent } from './portal/partner-stations.component';
import { PartnerChargePointsComponent } from './portal/partner-charge-points.component';
import { PartnerSessionsComponent } from './portal/partner-sessions.component';
import { PartnerStatisticsComponent } from './portal/partner-statistics.component';
import { PartnerProfileComponent } from './portal/partner-profile.component';

const routes: Routes = [
  { path: '', component: PartnerOverviewComponent },
  { path: 'stations', component: PartnerStationsComponent },
  { path: 'charge-points', component: PartnerChargePointsComponent },
  { path: 'sessions', component: PartnerSessionsComponent },
  { path: 'statistics', component: PartnerStatisticsComponent },
  { path: 'profile', component: PartnerProfileComponent },
  { path: 'security', component: TwoFactorSettingsComponent },
  // Former pages
  { path: 'connector-realtime', redirectTo: 'charge-points' },
  { path: 'charging-sessions', redirectTo: 'sessions' },
  { path: '**', redirectTo: '' }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class PartnerDashboardRoutingModule { }
