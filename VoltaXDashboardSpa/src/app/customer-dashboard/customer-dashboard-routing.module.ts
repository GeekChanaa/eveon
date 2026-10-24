import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CustomerOverviewComponent } from './portal/customer-overview.component';
import { CustomerSessionsComponent } from './portal/customer-sessions.component';
import { CustomerSessionDetailComponent } from './portal/customer-session-detail.component';
import { CustomerWalletComponent } from './portal/customer-wallet.component';
import { CustomerPaymentMethodsComponent } from './portal/customer-payment-methods.component';
import { CustomerCardsComponent, CustomerProfileComponent } from './portal/customer-account.component';

const routes: Routes = [
  { path: '', component: CustomerOverviewComponent },
  { path: 'sessions', component: CustomerSessionsComponent },
  { path: 'sessions/:id', component: CustomerSessionDetailComponent },
  { path: 'wallet', component: CustomerWalletComponent },
  { path: 'payment-methods', component: CustomerPaymentMethodsComponent },
  { path: 'cards', component: CustomerCardsComponent },
  { path: 'profile', component: CustomerProfileComponent },
  { path: '**', redirectTo: '' }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class CustomerDashboardRoutingModule { }
