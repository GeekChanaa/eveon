import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { AtomsModule } from 'src/app/atoms/atoms.module';
import { SharedModule } from 'src/app/shared/shared.module';
import { OcppLocalListChargePointsComponent } from './ocpp-local-list-charge-points/ocpp-local-list-charge-points.component';
import { OcppLocalListComponent } from './ocpp-local-list.component';
import { RouterModule } from '@angular/router';
import { OcppLocalListRoutingModule } from './ocpp-local-list-routing.module';

@NgModule({
    declarations: [
        OcppLocalListComponent,
        OcppLocalListChargePointsComponent,
        
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
  