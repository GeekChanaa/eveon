import { Component } from '@angular/core';
import { LEGAL } from '../legal';

@Component({
  selector: 'app-cookies-policy',
  templateUrl: './cookies-policy.component.html',
  styleUrls: ['../legal-page.sass']
})
export class CookiesPolicyComponent {
  readonly legal = LEGAL;
}
