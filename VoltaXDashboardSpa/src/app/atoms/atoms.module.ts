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
import { MatRippleModule } from '@angular/material/core';
import { SelectFormFieldComponent } from './select-form-field/select-form-field.component';
import { DebitCardComponent } from './debit-card/debit-card.component';
import { InvoiceComponent } from './invoice/invoice.component';
import { EmptyCardComponent } from './empty-card/empty-card.component';
import { MatButtonModule } from '@angular/material/button';
import { ActionModalComponent } from './action-modal/action-modal.component';
import { SvgSpinnerComponent } from './svg-spinner/svg-spinner.component';
import { MapPickerComponent } from './map-picker/map-picker.component';
import { DisplayItemComponent } from './display-item/display-item.component';
import { DisplayTableListComponent } from './display-table-list/display-table-list.component';

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
    RechargeCardComponent,
    SelectFormFieldComponent,
    DebitCardComponent,
    InvoiceComponent,
    EmptyCardComponent,
    ActionModalComponent,
    SvgSpinnerComponent,
    MapPickerComponent,
    DisplayItemComponent,
    DisplayTableListComponent
   ],
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    FormsModule,
    MatRippleModule,
    MatButtonModule
  ],
  exports: [
    CardComponent,
    TableListComponent,
    SearchInputComponent,
    SmallCardComponent,
    DisplayCellComponent,
    FormFieldComponent,
    RechargeCardComponent,
    SelectFormFieldComponent,
    DebitCardComponent,
    InvoiceComponent,
    EmptyCardComponent,
    ActionModalComponent,
    SvgSpinnerComponent,
    MapPickerComponent,
    DisplayItemComponent,
    DisplayTableListComponent,

  ],
  providers: [],
})
export class AtomsModule { }
