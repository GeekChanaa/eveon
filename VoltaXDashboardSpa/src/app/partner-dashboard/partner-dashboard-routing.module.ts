import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { PartnerOverviewComponent } from './portal/partner-overview.component';
import { PartnerStationsComponent } from './portal/partner-stations.component';
import { PartnerChargePointsComponent } from './portal/partner-charge-points.component';
import { PartnerSessionsComponent } from './portal/partner-sessions.component';
import { PartnerStatisticsComponent } from './portal/partner-statistics.component';
import { PartnerProfileComponent } from './portal/partner-profile.component';
import { PartnerStationDetailComponent } from './portal/partner-station-detail.component';
import { PartnerSecurityComponent } from './portal/partner-security.component';

const routes: Routes = [
  { path: '', component: PartnerOverviewComponent },
  { path: 'stations', component: PartnerStationsComponent },
  { path: 'stations/:id', component: PartnerStationDetailComponent },
  { path: 'charge-points', component: PartnerChargePointsComponent },
  { path: 'sessions', component: PartnerSessionsComponent },
  { path: 'statistics', component: PartnerStatisticsComponent },
  { path: 'statements', loadComponent: () => import('./portal/partner-statements.component').then(m => m.PartnerStatementsComponent) },
  { path: 'statements/:id', loadComponent: () => import('./portal/partner-statements.component').then(m => m.PartnerStatementsComponent) },
  { path: 'profile', component: PartnerProfileComponent },
  { path: 'security', component: PartnerSecurityComponent },
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
