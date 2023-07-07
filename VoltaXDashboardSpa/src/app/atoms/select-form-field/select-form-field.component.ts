import { Component, Input, OnInit } from '@angular/core';
import { FormGroup } from '@angular/forms';
@Component({
  selector: 'app-select-form-field',
  templateUrl: './select-form-field.component.html',
  styleUrls: ['./select-form-field.component.css']
})
export class SelectFormFieldComponent implements OnInit {

  @Input() parentForm!: FormGroup;
  @Input() controlName: string = "";
  @Input() options: {value: string, viewValue: string}[] = [];
  @Input() label: string = ""; 
  @Input() tooltip: string = "";

  ngOnInit(): void {
    
  }

}
