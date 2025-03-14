import { Component, Input, OnInit } from '@angular/core';
import { FormControl } from '@angular/forms';
import { ValidationMessagesService } from 'src/_services/validation-messages.service';

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
  @Input() errorMessage: string= "";
  
  constructor(
    private _validationMessagesService : ValidationMessagesService
  ) { }

  ngOnInit() {
  }

  get isInvalid() {
    return this.control.touched && this.control.invalid;
  }

  getErrorMessage(): string {
    if (!this.control.errors) return '';

    for (const errorKey in this.control.errors) {
      console.log("errorkey : ",errorKey);
      if (this.control.errors.hasOwnProperty(errorKey)) {
        return this._validationMessagesService.getMessage(errorKey, this.control.errors[errorKey]);
      }
    }
    return '';
  }

}
