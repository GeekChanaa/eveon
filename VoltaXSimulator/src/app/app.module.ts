import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { ChargingStationsComponent } from './charging-stations/charging-stations.component';
import { ChargePointsComponent } from './charge-points/charge-points.component';
import { AddChargePointComponent } from './add-charge-point/add-charge-point.component';
import { FormsModule } from '@angular/forms';
import { ChargePointSimComponent } from './charge-point-sim/charge-point-sim.component';

@NgModule({
  declarations: [				
    AppComponent,
      ChargingStationsComponent,
      ChargePointsComponent,
      AddChargePointComponent,
      ChargePointSimComponent
   ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    FormsModule,
    
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
