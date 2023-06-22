import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthComponent } from './auth/auth.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { AuthGuard } from 'src/_guards/auth.guard';

const routes: Routes = [
  {
    path: "",
    component: DashboardComponent,
    canActivate : [AuthGuard],
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
