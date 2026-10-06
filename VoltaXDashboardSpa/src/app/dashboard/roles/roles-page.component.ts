import { Component } from '@angular/core';
import { AtomsModule } from '../../atoms/atoms.module';
import { UserAccessComponent } from '../users/user-access/user-access.component';
import { PageState } from 'src/_models/_enums/page-state.enum';

@Component({
  selector: 'app-roles-page',
  standalone: true,
  imports: [AtomsModule, UserAccessComponent],
  template: `<app-dashboard-details-container [state]="ready" title="Roles & permissions"
    [breadcrumbs]="[{label: 'Dashboard', link: '/dashboard'}, {label: 'Roles & permissions'}]">
    <app-user-access [management]="true"></app-user-access>
  </app-dashboard-details-container>`
})
export class RolesPageComponent { readonly ready = PageState.Success; }
