import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthComponent } from './auth/auth.component';
import { DashboardComponent } from './dashboard/dashboard.component';

const routes: Routes = [
  {
    path: "",
    component: DashboardComponent,
    loadChildren : () => import('./dashboard/dashboard.module').then( m => m.DashboardModule)
  },
  {
    path : "auth",
    component : AuthComponent,
    loadChildren : () => import('./auth/auth.module')
      .then(m=>m.AuthModule)
  }
  
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
