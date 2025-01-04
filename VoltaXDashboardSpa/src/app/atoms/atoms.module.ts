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
import { ProgressBarComponent } from './progress-bar/progress-bar.component';
import { ConnectorStatusDescriptionComponent } from './connector-status-description/connector-status-description.component';
import { StoppedReasonDescriptionComponent } from './stopped-reason-description/stopped-reason-description.component';
import { ToggleComponent } from './toggle/toggle.component';
import { RatingComponent } from './rating/rating.component';
import { PreloaderContainerComponent } from './preloader-container/preloader-container.component';
import { ConnectorStatusDotComponent } from './connector-status-dot/connector-status-dot.component';
import { PaginationComponent } from './pagination/pagination.component';
import { GetBaseReportExplanationComponent } from './get-base-report-explanation/get-base-report-explanation.component';
import { StoppedReasonTagComponent } from './stopped-reason-tag/stopped-reason-tag.component';
import { CamelToKebabPipe } from 'src/_pipes/camel-to-kebab.pipe';
import { ChargingSessionStatusDescriptionComponent } from './charging-session-status-description/charging-session-status-description.component';
import { ChargingSessionStatusTagComponent } from './charging-session-status-tag/charging-session-status-tag.component';
import { EnumSelectComponent } from './enum-select/enum-select.component';
import { ToggleButtonsComponent } from './toggle-buttons/toggle-buttons.component';

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
    DisplayTableListComponent,
    ProgressBarComponent,
    ConnectorStatusDescriptionComponent,
    StoppedReasonDescriptionComponent,
    RatingComponent,
    ToggleComponent,
    PreloaderContainerComponent,
    ConnectorStatusDotComponent,
    PaginationComponent,
    GetBaseReportExplanationComponent,
    StoppedReasonTagComponent,
    CamelToKebabPipe,
    ChargingSessionStatusDescriptionComponent,
    ChargingSessionStatusTagComponent,
    EnumSelectComponent,
    ToggleButtonsComponent,
  ],
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    FormsModule,
    MatRippleModule,
    MatButtonModule,
    
  ],
  exports: [
    CardComponent,
    TableListComponent,
    SearchInputComponent,
    SmallCardComponent,
    DisplayCellComponent,
    FormFieldComponent,
    ToggleButtonsComponent,
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
    ProgressBarComponent,
    ConnectorStatusDescriptionComponent,
    StoppedReasonDescriptionComponent,
    ToggleComponent,
    RatingComponent,
    PreloaderContainerComponent,
    ConnectorStatusDotComponent,
    PaginationComponent,
    GetBaseReportExplanationComponent,
    StoppedReasonTagComponent,
    CamelToKebabPipe,
    ChargingSessionStatusDescriptionComponent,
    ChargingSessionStatusTagComponent,
    EnumSelectComponent
  ],
  providers: [],
})
export class AtomsModule { }
