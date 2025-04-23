import { Component, Input, OnInit } from '@angular/core';
import { FormControl, Validators } from '@angular/forms';

@Component({
  selector: 'app-field-label',
  templateUrl: './field-label.component.html',
  styleUrls: ['./field-label.component.sass']
})
export class FieldLabelComponent implements OnInit {
  
  @Input() title : string = "";
  @Input() description : string = "";
  @Input() control: FormControl = new FormControl('');

  constructor() { }

  ngOnInit() {
  }

  hasRequiredValidator(): boolean {
    return this.control && this.control.hasValidator(Validators.required);
  }

  

}
