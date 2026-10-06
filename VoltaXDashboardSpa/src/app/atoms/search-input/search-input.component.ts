import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { FormControl, Validators } from '@angular/forms';
import { ValidationMessagesService } from 'src/_services/validation-messages.service';

@Component({
  selector: 'app-search-input',
  templateUrl: './search-input.component.html',
  styleUrls: ['./search-input.component.sass']
})
export class SearchInputComponent implements OnInit, OnChanges {

  @Input() options: {id : any, name : any}[] = [];
  @Input() control: FormControl = new FormControl('');
  @Output() optionSelected = new EventEmitter<any>();
  @Output() ngModelChange = new EventEmitter<string>();
  @Input() placeholder : string = "Value"
  @Input() disabled : boolean = false;
  @Input() title : string = "";
  @Input() description : string = "";
  @Input() isError: boolean = false;


  searchString: string = "";

  // Add a new property for filtered options
  filteredOptions: any[] = [];


  showList = false;

  constructor(
    private _validationMessageService : ValidationMessagesService
  ) { }

  ngOnInit(): void {
    this.filteredOptions = this.options;
  }
  
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['options'] && this.options?.length) {
      this.filteredOptions = this.options;
  
      if (this.control?.value) {
        const selectedOption = this.options.find(option => option.id === this.control.value);
        if (selectedOption) {
          this.searchString = this.getOptionLabel(selectedOption);
        }
      }
    }
  }

  filterOptions() {
    if (!this.searchString || this.searchString.trim() === '') {
      this.filteredOptions = [...this.options];
    } else {
      this.filteredOptions = this.options.filter(option => 
        option.name.toLowerCase().includes(this.searchString.toLowerCase())
      );
    }
  }
  
  showSelect() {
    this.showList = true;
    this.filteredOptions = [...this.options];
  }

  hideSelect() {
    this.showList = false;
  }

  selectOption(option: any) {
    this.searchString = this.getOptionLabel(option);
    this.control.setValue(option.id);
    this.control.markAsTouched();
    this.showList = false;
    this.optionSelected.emit(option);
  }

  // Cities use name as id, so avoid showing "Tangier-Tangier"
  getOptionLabel(option: any): string {
    return option.id === option.name ? option.name : option.id + '-' + option.name;
  }

  getErrorMessage(): string {
    if (!this.control.errors) return '';

    for (const errorKey in this.control.errors) {
      console.log("errorkey : ",errorKey);
      if (this.control.errors.hasOwnProperty(errorKey)) {
        return this._validationMessageService.getMessage(errorKey, this.control.errors[errorKey]);
      }
    }
    return '';
  }

  hasRequiredValidator(): boolean {
    return this.control && this.control.hasValidator(Validators.required);
  }

  get isInvalid() {
    return this.control.touched && this.control.invalid;
  }

}
