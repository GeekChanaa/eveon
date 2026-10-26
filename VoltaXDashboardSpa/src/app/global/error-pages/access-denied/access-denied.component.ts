import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
@Component({ selector: 'app-access-denied', standalone: true, imports: [RouterModule, TranslateModule],
  template: `<div class="page page_simple"><div class="entry"><main class="error-page"><img class="some-icon" src="/assets/images/logos/logo.svg" alt="eveon" width="180"><img class="some-icon-dark" src="/assets/images/logos/logo-white.svg" alt="eveon" width="180"><span class="error-page__code">403</span><h1 class="error-page__title">{{ 'errors.accessDenied.title' | translate }}</h1><p class="error-page__text">{{ 'errors.accessDenied.text' | translate }}</p><a class="error-page__action" routerLink="/">{{ 'errors.accessDenied.home' | translate }}</a></main></div></div>` })
export class AccessDeniedComponent {}
