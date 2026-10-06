import { Component } from '@angular/core';
import { LEGAL } from '../legal';

@Component({
  selector: 'app-terms-conditions',
  templateUrl: './terms-conditions.component.html',
  styleUrls: ['../legal-page.sass']
})
export class TermsConditionsComponent {
  readonly legal = LEGAL;
}
