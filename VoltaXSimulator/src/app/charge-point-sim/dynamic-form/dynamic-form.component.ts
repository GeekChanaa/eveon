import { Component, Input, OnInit } from '@angular/core';
import {
  FormGroup,
  FormBuilder,
  AbstractControl,
  FormControl,
  FormArray,
} from '@angular/forms';

@Component({
  selector: 'app-dynamic-form',
  templateUrl: './dynamic-form.component.html',
  styleUrls: ['./dynamic-form.component.css'],
})
export class DynamicFormComponent implements OnInit {
  @Input() requestType: any;
  @Input() formGroup!: FormGroup;
  
  model : any = {};

  req : any = {};

  form!: FormGroup;
  fieldTypes: Record<
    string,
    'text' | 'select' | 'boolean' | 'object' | 'array'
  > = {};

  constructor(private fb: FormBuilder) {}

  ngOnInit() {
  }

  onSubmit() {
    console.log('this is the submit');
  }

  objectKeys(obj:any) : any[]{
    return Object.keys(obj);
  }

  test(){
    console.log(this.model);
  }
  
}
