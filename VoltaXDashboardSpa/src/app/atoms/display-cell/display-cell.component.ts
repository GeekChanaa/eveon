import { AfterViewInit, Component, ElementRef, EventEmitter, HostListener, Input, OnInit, Output, Renderer2, ViewChild } from '@angular/core';
import { FormControl } from '@angular/forms';
import { Observable } from 'rxjs';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { EnumMappingService } from 'src/_services/enum-mapping.service';

export interface SelectOption<T = string> {
  value: T;
  label: string;
  disabled?: boolean;
}
@Component({
  selector: 'app-display-cell',
  templateUrl: './display-cell.component.html',
  styleUrls: ['./display-cell.component.sass']
})
export class DisplayCellComponent implements OnInit, AfterViewInit {
@Input() title: string = '';
  @Input() val: any = '';
  @Input() object: any = {};
  @Input() inpType: string = 'text';
  @Input() enumName: string = '';
  @Input() tooltip: string = '';
  @Input() editable: boolean = true;
  @Input() isLink: boolean = false;
  @Input() link: string = '';
  @Input() options: SelectOption[] = [];
  @Input() control?: FormControl; // New FormControl input
  
  @Input() updateObservable!: (id: number, object: any) => Observable<any>;
  
  @Output() valueChanged = new EventEmitter<any>();
  
  @ViewChild('inputField', { static: false }) inputField!: ElementRef;
  
  updatedValue: any = '';
  editing: boolean = false;
  isLoading: boolean = false;
  errorMessage: string = '';
  originalValue: any = '';
  enumMappings: { [key: string]: { [id: number]: string } } = {};
  
  // Internal FormControl for cases where none is provided
  internalFormControl: FormControl = new FormControl();
  
  constructor(
    private _enumService: EnumMappingService,
    private _modalService: ActionModalService,
    private _elRef: ElementRef
  ) {}
  
  ngOnInit() {
    this.updatedValue = this.val ? this.val : '';
    this.originalValue = this.val ? this.val : '';
    
    // Use provided FormControl or create internal one
    if (!this.control) {
      this.internalFormControl.setValue(this.val);
    } else {
      // Sync FormControl value with component value
      if (this.control.value !== this.val) {
        this.control.setValue(this.val);
      }
    }
    
    if (this.inpType === 'select_enum') {
      this.enumMappings = this._enumService.getEnumMapping(this.enumName);
    }
  }
  
  ngAfterViewInit() {
    // Focus input field when editing starts
    if (this.editing && this.inputField) {
      setTimeout(() => {
        this.inputField.nativeElement.focus();
      }, 0);
    }
  }
  
  // Get the active FormControl (provided or internal)
  get activeFormControl(): FormControl {
    return this.control || this.internalFormControl;
  }
  
  // Check if the field has validation errors
  get hasErrors(): boolean {
    const control = this.activeFormControl;
    return !!(control && control.invalid && (control.dirty || control.touched));
  }
  
  // Get validation error messages
  get validationErrors(): string[] {
    const control = this.activeFormControl;
    const errors: string[] = [];
    
    if (control && control.errors) {
      if (control.errors['required']) {
        errors.push(`${this.title} is required`);
      }
      if (control.errors['email']) {
        errors.push(`${this.title} must be a valid email address`);
      }
      if (control.errors['minlength']) {
        const requiredLength = control.errors['minlength'].requiredLength;
        errors.push(`${this.title} must be at least ${requiredLength} characters long`);
      }
      if (control.errors['maxlength']) {
        const requiredLength = control.errors['maxlength'].requiredLength;
        errors.push(`${this.title} must be no more than ${requiredLength} characters long`);
      }
      if (control.errors['min']) {
        const min = control.errors['min'].min;
        errors.push(`${this.title} must be at least ${min}`);
      }
      if (control.errors['max']) {
        const max = control.errors['max'].max;
        errors.push(`${this.title} must be no more than ${max}`);
      }
      if (control.errors['pattern']) {
        errors.push(`${this.title} format is invalid`);
      }
      // Custom error messages
      if (control.errors['custom']) {
        errors.push(control.errors['custom']);
      }
    }
    
    return errors;
  }
  
  startEditing() {
    this.errorMessage = '';
    this.editing = true;
    this.updatedValue = this.val;
    
    // Update FormControl value
    this.activeFormControl.setValue(this.updatedValue);
    this.activeFormControl.markAsTouched();
    
    // Set focus on input after view is updated
    setTimeout(() => {
      if (this.inputField) {
        this.inputField.nativeElement.focus();
        if (this.inpType === 'text' || this.inpType === 'number') {
          this.inputField.nativeElement.select();
        }
      }
    }, 0);
  }
  
  cancelEditing() {
    this.updatedValue = this.originalValue;
    this.editing = false;
    this.errorMessage = '';
    
    // Reset FormControl
    this.activeFormControl.setValue(this.originalValue);
    this.activeFormControl.markAsUntouched();
  }
  
  getEnumKeys() {
    return Object.keys(this.enumMappings);
  }
  
  getEnumValues() {
    return Object.values(this.enumMappings);
  }
  
  // Handle input changes and sync with FormControl
  onInputChange(event: Event): void {
    const input = event.target as HTMLInputElement | null;
    const value = input?.value ?? '';
    this.updatedValue = value;
    this.activeFormControl.setValue(value, { emitEvent: true });
    this.activeFormControl.markAsTouched();
  }

  onCheckedChange(event: Event): void {
    const input = event.target as HTMLInputElement | null;
    const checked = input?.checked ?? false;

    this.updatedValue = checked;
    this.activeFormControl.setValue(checked, { emitEvent: true });
    this.activeFormControl.markAsTouched();
  }
  
  updateVal() {
    // Update FormControl and trigger validation
    if(this.updatedValue == this.val)
    {
      this.editing = false;
      return;
    }
    this.activeFormControl.setValue(this.updatedValue);
    this.activeFormControl.markAsTouched();
    
    // Check if FormControl is valid
    if (this.activeFormControl.invalid) {
      this.errorMessage = this.validationErrors.join(', ');
      return;
    }
    
    // Basic validation (keeping existing logic as fallback)
    if (this.inpType === 'text' && this.updatedValue === '') {
      this.errorMessage = `${this.title} cannot be empty`;
      return;
    }
    
    // Update the object with new value
    this.object[this.title] = this.updatedValue;
    this.isLoading = true;
    
    // Call API to update
    this.updateObservable(this.object.id, this.object).subscribe(
      (data) => {
        this.isLoading = false;
        this.originalValue = this.updatedValue;
        this.val = this.updatedValue;
        this.editing = false;
        this.errorMessage = '';
        
        // Mark FormControl as pristine after successful update
        this.activeFormControl.markAsPristine();
        
        // Notify parent component
        this.valueChanged.emit({
          field: this.title,
          value: this.updatedValue,
          object: this.object
        });
        
        this._modalService.popup(
          ActionModalStatusEnum.Success,
          "Success!",
          `${this.title} updated successfully`,
          4000
        );
      },
      (error) => {
        this.isLoading = false;
        this.errorMessage = error?.message || 'Something went wrong. Please try again later.';
        
        this._modalService.popup(
          ActionModalStatusEnum.Error,
          "Error!",
          this.errorMessage,
          4000
        );
      }
    );
  }
  
  @HostListener('document:keydown.enter', ['$event'])
  handleEnter(event: KeyboardEvent) {
    if (this.editing && this.inputField && document.activeElement === this.inputField.nativeElement) {
      this.updateVal();
    }
  }
  

}
