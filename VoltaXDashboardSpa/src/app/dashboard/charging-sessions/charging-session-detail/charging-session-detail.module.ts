import { NgModule } from '@angular/core';
import { AtomsModule } from 'src/app/atoms/atoms.module';
import { PortalUiModule } from 'src/app/shared/portal-ui/portal-ui.module';
import { ChargingSessionDetailComponent } from './charging-session-detail.component';

/** The operator's session page, shared by the sessions list and the charge point pages. */
@NgModule({
  declarations: [ChargingSessionDetailComponent],
  imports: [AtomsModule, PortalUiModule],
  exports: [ChargingSessionDetailComponent]
})
export class ChargingSessionDetailModule { }
