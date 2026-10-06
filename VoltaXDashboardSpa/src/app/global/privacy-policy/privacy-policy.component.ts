import { Component } from '@angular/core';
import { LEGAL } from '../legal';

@Component({
  selector: 'app-privacy-policy',
  templateUrl: './privacy-policy.component.html',
  styleUrls: ['../legal-page.sass']
})
export class PrivacyPolicyComponent {
  readonly legal = LEGAL;
}
