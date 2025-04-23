import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-documentation-buttons',
  templateUrl: './documentation-buttons.component.html',
  styleUrls: ['./documentation-buttons.component.sass']
})
export class DocumentationButtonsComponent {

  // For event demonstration
  lastEvent: string = 'No events triggered yet';
  eventCount: number = 0;

  // Demo function for tracking button events
  logEvent(eventName: string, event: any): void {
    this.lastEvent = `${eventName} triggered (${++this.eventCount})`;
    console.log(`Button event: ${eventName}`, event);
  }

  // For disabled state toggle demo
  isDisabled = true;
  toggleDisabled(): void {
    this.isDisabled = !this.isDisabled;
  }

  // For form submission demo
  formSubmitted = false;
  onFormSubmit(): void {
    this.formSubmitted = true;
    setTimeout(() => {
      this.formSubmitted = false;
    }, 2000);
  }
}
