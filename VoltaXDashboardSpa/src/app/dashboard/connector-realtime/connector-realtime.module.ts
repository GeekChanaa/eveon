import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { AtomsModule } from 'src/app/atoms/atoms.module';
import { SharedModule } from 'src/app/shared/shared.module';
import { ConnectorRealtimeRoutingModule } from './connector-realtime-routing.module';
import { ConnectorRealtimeComponent } from './connector-realtime.component';
import { ConnectorRealtimeListComponent } from './connector-realtime-list/connector-realtime-list.component';
import { ConnectorRealtimeActionsComponent } from './connector-realtime-actions/connector-realtime-actions.component';
import { AppTableCustomButtonDirective } from 'src/_directives/table-custom-button.directive';
import { RequestHandlerComponent } from './connector-realtime-actions/request-handler/request-handler.component';

@NgModule({
    declarations: [
      ConnectorRealtimeComponent,
      ConnectorRealtimeListComponent,
      ConnectorRealtimeActionsComponent,
      AppTableCustomButtonDirective,
      RequestHandlerComponent
  ],
    imports: [
        AtomsModule,
        ReactiveFormsModule,
        CommonModule,
        SharedModule,
        FormsModule,
        ConnectorRealtimeRoutingModule
    ],
  })
  export class ConnectorRealtimeModule { }
  