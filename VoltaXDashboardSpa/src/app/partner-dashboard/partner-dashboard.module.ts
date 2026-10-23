import { NgModule } from '@angular/core';
import { NgApexchartsModule } from 'ng-apexcharts';
import { SharedModule } from '../shared/shared.module';
import { PortalUiModule } from '../shared/portal-ui/portal-ui.module';
import { PartnerDashboardRoutingModule } from './partner-dashboard-routing.module';
import { TwoFactorSettingsModule } from '../dashboard/profile/profile-security/two-factor-settings/two-factor-settings.module';
import { PartnerOverviewComponent } from './portal/partner-overview.component';
import { PartnerStationsComponent } from './portal/partner-stations.component';
import { PartnerChargePointsComponent } from './portal/partner-charge-points.component';
import { PartnerPricingEditorComponent } from './portal/partner-pricing-editor.component';
import { PartnerSessionsComponent } from './portal/partner-sessions.component';
import { PartnerStatisticsComponent } from './portal/partner-statistics.component';
import { PartnerProfileComponent } from './portal/partner-profile.component';
import { PartnerLogoComponent } from './portal/partner-logo.component';

/** The partner portal: the partner's own stations, prices, sessions and revenue (api/partner-portal). */
@NgModule({
  declarations: [
    PartnerOverviewComponent,
    PartnerStationsComponent,
    PartnerChargePointsComponent,
    PartnerPricingEditorComponent,
    PartnerSessionsComponent,
    PartnerStatisticsComponent,
    PartnerProfileComponent,
    PartnerLogoComponent
  ],
  imports: [
    SharedModule,
    PortalUiModule,
    NgApexchartsModule,
    TwoFactorSettingsModule,
    PartnerDashboardRoutingModule
  ]
})
export class PartnerDashboardModule { }
