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

  form!: FormGroup;
  fieldTypes: Record<
    string,
    'text' | 'select' | 'boolean' | 'object' | 'array'
  > = {};

  constructor(private fb: FormBuilder) {}

  ngOnInit() {
    this.form = this.createGroup(this.requestType);
  }

  createGroup(obj: any): FormGroup {
    const group = this.fb.group({});
    Object.keys(obj).forEach((key) => {
      if (obj[key].type === 'enum') {
        group.addControl(key, this.fb.control(''));
      } else if (obj[key].type === 'string' || obj[key].type === 'number') {
        group.addControl(key, this.fb.control(''));
      } else if (obj[key].type === 'object') {
        group.addControl(key, this.createGroup(obj[key].value));
      } else if (obj[key].type === 'array') {
        group.addControl(
          key,
          this.fb.array([this.createGroup(obj[key].value)])
        );
      }
    });
    return group;
  }

  onSubmit() {
    console.log('this is the submit');
  }

  objectKeys(obj:any) {
    return Object.keys(obj);
  }
  
  getType(control:any) {
    if (control instanceof FormGroup) {
      return 'FormGroup';
    } else if (control instanceof FormArray) {
      return 'FormArray';
    } else {
      return 'FormControl';
    }
  }
  
}
