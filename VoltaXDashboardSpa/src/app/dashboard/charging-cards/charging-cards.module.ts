import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { AtomsModule } from 'src/app/atoms/atoms.module';
import { SharedModule } from 'src/app/shared/shared.module';
import { ChargingCardsComponent } from './charging-cards.component';
import { ChargingCardComponent } from './charging-card/charging-card.component';
import { ChargingCardOrdersComponent } from './charging-card-orders/charging-card-orders.component';
import { ChargingCardTransactionsComponent } from './charging-card-transactions/charging-card-transactions.component';
import { ChargingCardsListComponent } from './charging-cards-list/charging-cards-list.component';
import { ChargingCardsRoutingModule } from './charging-cards-routing.module';
import { CreateChargingCardComponent } from './create-charging-card/create-charging-card.component';
import { ChargingCardHistoryComponent } from './charging-card-history/charging-card-history.component';


@NgModule({
  declarations: [
    ChargingCardsComponent,
    ChargingCardComponent,
    ChargingCardOrdersComponent,
    ChargingCardTransactionsComponent,
    ChargingCardsListComponent,
    CreateChargingCardComponent,
    ChargingCardHistoryComponent
  ],
  imports: [
      AtomsModule,
      ReactiveFormsModule,
      CommonModule,
      SharedModule,
      FormsModule,
      ChargingCardsRoutingModule
  ],
})
export class ChargingCardsModule { }
