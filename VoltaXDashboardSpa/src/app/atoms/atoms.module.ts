import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { RouterModule } from '@angular/router';
import { AtomsComponent } from './atoms.component';
import { CardComponent } from './card/card.component';
import { TableListComponent } from './table-list/table-list.component';

@NgModule({
  declarations: [
    AtomsComponent,
    CardComponent,
    TableListComponent
   ],
  imports: [
    CommonModule,
    RouterModule,
  ],
  exports: [
    CardComponent,
    TableListComponent
  ],
  providers: [],
})
export class AtomsModule { }
