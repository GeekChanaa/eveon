import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { RouterModule } from '@angular/router';
import { AtomsComponent } from './atoms.component';
import { CardComponent } from './card/card.component';
import { TableListComponent } from './table-list/table-list.component';
import { SearchInputComponent } from './search-input/search-input.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

@NgModule({
  declarations: [
    AtomsComponent,
    CardComponent,
    TableListComponent,
    SearchInputComponent
   ],
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,

  ],
  exports: [
    CardComponent,
    TableListComponent,
    SearchInputComponent,
  ],
  providers: [],
})
export class AtomsModule { }
