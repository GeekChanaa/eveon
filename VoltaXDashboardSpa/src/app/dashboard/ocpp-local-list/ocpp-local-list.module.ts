import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { AtomsModule } from 'src/app/atoms/atoms.module';
import { SharedModule } from 'src/app/shared/shared.module';
import { OcppLocalListChargePointsComponent } from './ocpp-local-list-charge-points/ocpp-local-list-charge-points.component';
import { OcppLocalListComponent } from './ocpp-local-list.component';
import { RouterModule } from '@angular/router';
import { OcppLocalListRoutingModule } from './ocpp-local-list-routing.module';
import { AppTableCustomButtonDirective } from 'src/_directives/table-custom-button.directive';
import { OcppLocalListChargePointComponent } from './ocpp-local-list-charge-point/ocpp-local-list-charge-point.component';

@NgModule({
    declarations: [
        OcppLocalListComponent,
        OcppLocalListChargePointsComponent,
        OcppLocalListChargePointComponent,
        AppTableCustomButtonDirective
    ],
    imports: [
        AtomsModule,
        ReactiveFormsModule,
        CommonModule,
        SharedModule,
        FormsModule,
        RouterModule,
        OcppLocalListRoutingModule
    ],
  })
  export class OcppLocalListModule { }
  