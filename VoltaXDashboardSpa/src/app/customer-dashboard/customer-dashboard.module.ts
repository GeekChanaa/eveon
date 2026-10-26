import { NgModule } from '@angular/core';
import { SharedModule } from '../shared/shared.module';
import { PortalUiModule } from '../shared/portal-ui/portal-ui.module';
import { CustomerDashboardRoutingModule } from './customer-dashboard-routing.module';
import { CustomerOverviewComponent } from './portal/customer-overview.component';
import { CustomerSessionsComponent } from './portal/customer-sessions.component';
import { CustomerSessionDetailComponent } from './portal/customer-session-detail.component';
import { CustomerWalletComponent } from './portal/customer-wallet.component';
import { CustomerPaymentMethodsComponent } from './portal/customer-payment-methods.component';
import { CustomerCardsComponent, CustomerProfileComponent } from './portal/customer-account.component';
import { CustomerLiveSessionComponent } from './portal/customer-live-session.component';
import { ProfileCompletionBannerComponent } from './profile-completion/profile-completion-banner.component';
import { CustomerContactFormComponent } from './profile-completion/customer-contact-form.component';
import { CustomerDetailsFormComponent } from './profile-completion/customer-details-form.component';

/** The customer portal: the signed-in customer's own data, read only, plus paying online and completing the profile. */
@NgModule({
  declarations: [
    CustomerOverviewComponent,
    CustomerSessionsComponent,
    CustomerSessionDetailComponent,
    CustomerWalletComponent,
    CustomerPaymentMethodsComponent,
    CustomerCardsComponent,
    CustomerProfileComponent,
    CustomerLiveSessionComponent
  ],
  imports: [
    SharedModule,
    PortalUiModule,
    CustomerDashboardRoutingModule,
    ProfileCompletionBannerComponent,
    CustomerContactFormComponent,
    CustomerDetailsFormComponent
  ]
})
export class CustomerDashboardModule { }
