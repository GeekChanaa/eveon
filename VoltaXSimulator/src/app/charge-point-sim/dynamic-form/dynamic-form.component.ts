import { Component, Input, OnInit } from '@angular/core';
import { FormGroup, FormBuilder, AbstractControl } from '@angular/forms';

@Component({
  selector: 'app-dynamic-form',
  templateUrl: './dynamic-form.component.html',
  styleUrls: ['./dynamic-form.component.css']
})
export class DynamicFormComponent implements OnInit {


  @Input() requestType: any;
  @Input() formGroup!: FormGroup; 

  dynamicForm!: FormGroup;
  fieldTypes: Record<string, 'text' | 'select' | 'boolean' | 'object' | 'array'> = {};

  constructor(private fb: FormBuilder) { }

  ngOnInit(): void {
    
    
      console.log("this is the form group not null");
      this.dynamicForm = this.createForm(this.requestType);
     

  }

  createForm(requestType: any): FormGroup {
    const group = this.fb.group({});
    for (const field in requestType) {
      if (requestType.hasOwnProperty(field)) {
        if (Array.isArray(requestType[field]) && typeof requestType[field][0] == "string") {
          this.fieldTypes[field] = 'array';
          const formGroups = requestType[field].map((item:any) => {
            this.createForm(item)
          });
          group.addControl(field, this.fb.control(requestType[field]));
        } 
        else if (typeof requestType[field] === 'object' && requestType[field] !== null) {
          this.fieldTypes[field] = 'object';
          var igroup = (this.createForm(requestType[field]))
          group.addControl(field, igroup);
        } 
        else if (typeof requestType[field] === 'boolean') {
          this.fieldTypes[field] = 'boolean';
          group.addControl(field, this.fb.control(requestType[field] ? 'yes' : 'no'));
        } 
        else {
          this.fieldTypes[field] = 'text';
          group.addControl(field, this.fb.control(requestType[field]));
        }
      }
    }
    return group;
  }

  getFormGroup(fieldName: string): FormGroup {
    return this.dynamicForm.get(fieldName) as FormGroup;
  }

  onSubmit(){
    console.log("this is the submit");
  }

}
