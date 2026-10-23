import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
@Component({ selector: 'app-access-denied', standalone: true, imports: [RouterModule, TranslateModule],
  template: `<main><span>403</span><h1>{{ 'errors.accessDenied.title' | translate }}</h1><p>{{ 'errors.accessDenied.text' | translate }}</p><a routerLink="/">{{ 'errors.accessDenied.home' | translate }}</a></main>`,
  styles: [`main { max-width: 520px; margin: 15vh auto; padding: 32px; text-align: center; } span { color: #1F3461; font-weight: 600; } h1 { font-size: 28px; margin: 16px 0; } p { line-height: 1.7; } a { display:inline-block; margin-top:24px; padding:12px 20px; border-radius:8px; background:#0A1A3A; color:#fff; }`] })
export class AccessDeniedComponent {}
