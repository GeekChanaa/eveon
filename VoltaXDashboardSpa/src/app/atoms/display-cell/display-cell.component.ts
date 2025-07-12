import { AfterViewInit, Component, ElementRef, EventEmitter, HostListener, Input, OnInit, Output, Renderer2, ViewChild } from '@angular/core';
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
  @Input() options : SelectOption[] = [];
  
  @Input() updateObservable!: (id: number, object: any) => Observable<any>;
  
  @Output() valueChanged = new EventEmitter<any>();
  
  @ViewChild('inputField', { static: false }) inputField!: ElementRef;
  
  updatedValue: any = '';
  editing: boolean = false;
  isLoading: boolean = false;
  errorMessage: string = '';
  originalValue: any = '';
  enumMappings: { [key: string]: { [id: number]: string } } = {};
  
  constructor(
    private _enumService: EnumMappingService,
    private _modalService: ActionModalService,
    private _elRef: ElementRef
  ) {}
  
  ngOnInit() {
    this.updatedValue = this.val ? this.val : '';
    this.originalValue = this.val ? this.val : '';
    
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
  
  startEditing() {
    this.errorMessage = '';
    this.editing = true;
    this.updatedValue = this.val;
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
  }
  
  getEnumKeys() {
    return Object.keys(this.enumMappings);
  }
  
  getEnumValues() {
    return Object.values(this.enumMappings);
  }
  
  updateVal() {
    // Basic validation
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
