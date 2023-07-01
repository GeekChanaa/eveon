import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { RouterModule } from '@angular/router';
import { AtomsComponent } from './atoms.component';
import { CardComponent } from './card/card.component';
import { TableListComponent } from './table-list/table-list.component';
import { SearchInputComponent } from './search-input/search-input.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { SmallCardComponent } from './small-card/small-card.component';
import { CamelCaseToSpacePipe } from 'src/pipes/camel-case-to-space-case.pipe';
import { DisplayCellComponent } from './display-cell/display-cell.component';
import { FormFieldComponent } from './form-field/form-field.component';
import { RechargeCardComponent } from './recharge-card/recharge-card.component';

@NgModule({
  declarations: [
    AtomsComponent,
    CardComponent,
    TableListComponent,
    SearchInputComponent,
    SmallCardComponent,
    CamelCaseToSpacePipe,
    DisplayCellComponent,
    FormFieldComponent,
    RechargeCardComponent
   ],
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    FormsModule
  ],
  exports: [
    CardComponent,
    TableListComponent,
    SearchInputComponent,
    SmallCardComponent,
    DisplayCellComponent,
    FormFieldComponent,
    RechargeCardComponent
  ],
  providers: [],
})
export class AtomsModule { }
