import { Component, Input, OnInit } from '@angular/core';
import { FormControl } from '@angular/forms';

@Component({
  selector: 'app-form-field',
  templateUrl: './form-field.component.html',
  styleUrls: ['./form-field.component.sass']
})
export class FormFieldComponent implements OnInit {

  @Input() title : string = "";
  @Input() description : string = "";
  @Input() fcName : string = "";
  @Input() control: FormControl = new FormControl('');
  @Input() loading: boolean = false;
  @Input() isError: boolean = false;
  @Input() errorMessage: string= "";
  
  constructor() { }

  ngOnInit() {
  }

  get isInvalid() {
    return this.control.touched && this.control.invalid;
  }

}
