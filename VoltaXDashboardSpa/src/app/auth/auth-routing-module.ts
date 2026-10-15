import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { GoogleCallbackComponent } from './google-callback/google-callback.component';
import { SessionComponent } from './session/session.component';
import { authSiteRedirect } from 'src/_guards/auth-site.guard';

// Sign in, sign up and the other auth pages live on the public site (environment.authUrl).
// Only the pages that act on this app's own session stay here.
export const AuthRoutes: Routes= [
  { path : 'session' , component : SessionComponent },
  // Comes back here after linking a Google account from the profile page
  { path : 'google-callback' , component : GoogleCallbackComponent },
  { path : '**' , canActivate : [authSiteRedirect], children : [] },
]

@NgModule({
  imports: [RouterModule.forChild(AuthRoutes)],
  exports: [RouterModule],
})
export class AuthRoutingModule{
}
