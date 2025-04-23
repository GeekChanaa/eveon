import { Component, Input, OnInit } from '@angular/core';
import { FormControl, Validators } from '@angular/forms';
import { EnumMappingService } from 'src/_services/enum-mapping.service';
import { ValidationMessagesService } from 'src/_services/validation-messages.service';

export interface SelectOption<T = string> {
  value: T;
  label: string;
  disabled?: boolean;
}
@Component({
  selector: 'app-form-field',
  templateUrl: './form-field.component.html',
  styleUrls: ['./form-field.component.sass']
})
export class FormFieldComponent implements OnInit {

  @Input() title : string = "";
  @Input() placeholder : string = "Value";
  @Input() description : string = "";
  @Input() inputType : string = "text";
  @Input() fcName : string = "";
  @Input() control: FormControl = new FormControl('');
  @Input() loading: boolean = false;
  @Input() isError: boolean = false;
  @Input() disabled: boolean = false;
  @Input() errorMessage: string= "";
  @Input() options : SelectOption[] = [];
  @Input() enumName : string = "";
  enumMappings: { [key: string]: { [id: number]: string } } = {}
  
  constructor(
    private _validationMessagesService : ValidationMessagesService,
    private _enumService : EnumMappingService
  ) { }

  ngOnInit() {
    if(this.inputType == 'select_enum'){
      this.enumMappings = this._enumService.getEnumMapping(this.enumName);
    }
  }

  get isInvalid() {
    return this.control.touched && this.control.invalid;
  }

  getErrorMessage(): string {
    if (!this.control.errors) return '';
    for (const errorKey in this.control.errors) {
      if (this.control.errors.hasOwnProperty(errorKey)) {
        return this._validationMessagesService.getMessage(errorKey, this.control.errors[errorKey]);
      }
    }
    return '';
  }

  getEnumValues() {
    return Object.values(this.enumMappings);
  }

  hasRequiredValidator(): boolean {
    return this.control && this.control.hasValidator(Validators.required);
  }
}
