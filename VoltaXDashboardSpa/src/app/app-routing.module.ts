import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthComponent } from './auth/auth.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { AuthGuard } from 'src/_guards/auth.guard';
import { CustomerDashboardComponent } from './customer-dashboard/customer-dashboard.component';
import { GlobalComponent } from './global/global.component';
import { NotFoundComponent } from './global/error-pages/not-found/not-found.component';
import { PartnerDashboardComponent } from './partner-dashboard/partner-dashboard.component';
import { GoodByeComponent } from './good-bye/good-bye.component';
import { PartnerAuthComponent } from './partner-auth/partner-auth.component';

const routes: Routes = [
  {
    path: "dashboard",
    component: DashboardComponent,
    canActivate : [AuthGuard],
    loadChildren : () => import('./dashboard/dashboard.module').then( m => m.DashboardModule)
  },
  {
    path: "my-dashboard",
    component: CustomerDashboardComponent,
    canActivate : [AuthGuard],
    loadChildren : () => import('./customer-dashboard/customer-dashboard.module').then( m => m.CustomerDashboardModule)
  },{
    path: "partner-dashboard",
    component: PartnerDashboardComponent,
    canActivate : [AuthGuard],
    loadChildren : () => import('./partner-dashboard/partner-dashboard.module').then( m => m.PartnerDashboardModule)
  },
  {
    path : "auth",
    component : AuthComponent,
    loadChildren : () => import('./auth/auth.module')
      .then(m=>m.AuthModule)
  },
  {
    path : "partner-auth",
    component : PartnerAuthComponent,
    loadChildren : () => import('./partner-auth/partner-auth.module')
      .then(m=>m.PartnerAuthModule)
  },
  {
    path : "",
    component : GlobalComponent,
    loadChildren : () => import('./global/global.module')
      .then(m=>m.GlobalModule)
  },
  { 
    path: 'goodbye', 
    component: GoodByeComponent 
  },
  {
    path : "**",
    component : NotFoundComponent,
  },

  
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
