import { Component } from '@angular/core';
import { AtomsModule } from '../../atoms/atoms.module';
import { PageState } from 'src/_models/_enums/page-state.enum';
import { ChargerEventsComponent } from './charger-events.component';

/** Every charger's OCPP 2.0.1 events (NotifyEvent); live alarms are pushed over the charger hub. */
@Component({
  selector: 'app-charger-events-page',
  standalone: true,
  imports: [AtomsModule, ChargerEventsComponent],
  template: `
    <app-dashboard-details-container [state]="ready" title="Charger events"
      [breadcrumbs]="[{ label: 'Dashboard', link: '/dashboard' }, { label: 'Charger events' }]">
      <app-charger-events></app-charger-events>
    </app-dashboard-details-container>`
})
export class ChargerEventsPageComponent {
  readonly ready = PageState.Success;
}
