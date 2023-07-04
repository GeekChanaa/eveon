import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { ChargingStationsComponent } from './charging-stations/charging-stations.component';
import { ChargePointsComponent } from './charge-points/charge-points.component';
import { AddChargePointComponent } from './add-charge-point/add-charge-point.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ChargePointSimComponent } from './charge-point-sim/charge-point-sim.component';
import { HttpClientModule } from '@angular/common/http';
import { DynamicFormComponent } from './charge-point-sim/dynamic-form/dynamic-form.component';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

@NgModule({
  declarations: [				
    AppComponent,
      ChargingStationsComponent,
      ChargePointsComponent,
      AddChargePointComponent,
      ChargePointSimComponent,
      DynamicFormComponent
   ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    FormsModule,
    HttpClientModule,
    BrowserAnimationsModule,
    ReactiveFormsModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
