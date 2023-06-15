import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { ChargingStationsComponent } from './charging-stations/charging-stations.component';
import { ChargePointsComponent } from './charge-points/charge-points.component';
import { AddChargePointComponent } from './add-charge-point/add-charge-point.component';

@NgModule({
  declarations: [			
    AppComponent,
      ChargingStationsComponent,
      ChargePointsComponent,
      AddChargePointComponent
   ],
  imports: [
    BrowserModule,
    AppRoutingModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
