import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import {MatButtonModule} from '@angular/material/button'; 
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
import { UpdateTransactionRequestComponent } from './charge-point-sim/update-transaction-request/update-transaction-request.component';
import { StopTransactionRequestComponent } from './charge-point-sim/stop-transaction-request/stop-transaction-request.component';
import { StatusNotificationRequestComponent } from './charge-point-sim/status-notification-request/status-notification-request.component';
import { StartTransactionRequestComponent } from './charge-point-sim/start-transaction-request/start-transaction-request.component';
import { MeterValuesRequestComponent } from './charge-point-sim/meter-values-request/meter-values-request.component';
import { HeartbeatRequestComponent } from './charge-point-sim/heartbeat-request/heartbeat-request.component';
import {MatExpansionModule} from '@angular/material/expansion'; 
import { MatFormFieldModule } from '@angular/material/form-field';
import {MatSelectModule} from '@angular/material/select'; 
@NgModule({
  declarations: [				
    AppComponent,
      ChargingStationsComponent,
      ChargePointsComponent,
      AddChargePointComponent,
      ChargePointSimComponent,
      DynamicFormComponent,
      UpdateTransactionRequestComponent,
      StopTransactionRequestComponent,
      StatusNotificationRequestComponent,
      StartTransactionRequestComponent,
      MeterValuesRequestComponent,
      HeartbeatRequestComponent,
      
   ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    FormsModule,
    HttpClientModule,
    BrowserAnimationsModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatExpansionModule,
    MatFormFieldModule,
    MatSelectModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
