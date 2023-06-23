import { Component, Input, OnInit } from '@angular/core';
import { FormControl } from '@angular/forms';

@Component({
  selector: 'app-form-field',
  templateUrl: './form-field.component.html',
  styleUrls: ['./form-field.component.css']
})
export class FormFieldComponent implements OnInit {

  @Input() title : string = "";
  @Input() description : string = "";
  @Input() fcName : string = "";
  @Input() control: FormControl = new FormControl('');
  
  constructor() { }

  ngOnInit() {
  }

}
