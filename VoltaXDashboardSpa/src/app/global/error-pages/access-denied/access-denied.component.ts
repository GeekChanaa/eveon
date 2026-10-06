import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
@Component({ selector: 'app-access-denied', standalone: true, imports: [RouterModule],
  template: `<main><span>403</span><h1>Access restricted</h1><p>Your role does not grant access to this page. Ask an administrator if you need access.</p><a routerLink="/">Return home</a></main>`,
  styles: [`main { max-width: 520px; margin: 15vh auto; padding: 32px; text-align: center; } span { color: #a77b10; font-weight: 600; } h1 { font-size: 28px; margin: 16px 0; } p { line-height: 1.7; } a { display:inline-block; margin-top:24px; padding:12px 20px; border-radius:8px; background:#e5b223; color:#171b24; }`] })
export class AccessDeniedComponent {}
