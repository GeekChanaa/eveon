import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { AtomsModule } from 'src/app/atoms/atoms.module';
import { SharedModule } from 'src/app/shared/shared.module';
import { PartnersRoutingModule } from './partners-routing.module';
import { PartnersListComponent } from './partners-list/partners-list.component';
import { PartnersComponent } from './partners.component';
import { PartnerComponent } from './partner/partner.component';
import { CreatePartnerComponent } from './create-partner/create-partner.component';
import { PartnerChargingStationsComponent } from './partner-charging-stations/partner-charging-stations.component';
import { PartnerChargingStationComponent } from './partner-charging-station/partner-charging-station.component';
@NgModule({
    declarations: [
      PartnersListComponent,
      PartnerComponent,
      CreatePartnerComponent,
      PartnerChargingStationsComponent,
      PartnerChargingStationsComponent,
      PartnerChargingStationComponent
    ],
    imports: [
        AtomsModule,
        ReactiveFormsModule,
        CommonModule,
        SharedModule,
        FormsModule,
        PartnersRoutingModule
    ],
  })
  export class PartnersModule { }
  