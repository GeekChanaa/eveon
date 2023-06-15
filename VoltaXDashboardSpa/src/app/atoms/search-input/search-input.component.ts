import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormControl } from '@angular/forms';

@Component({
  selector: 'app-search-input',
  templateUrl: './search-input.component.html',
  styleUrls: ['./search-input.component.css']
})
export class SearchInputComponent implements OnInit {

  @Input() options: string[] = [];
  @Input() control: FormControl = new FormControl('');
  @Output() optionSelected = new EventEmitter<string>();

  // Add a new property for filtered options
  filteredOptions: string[] = [];


  showList = false;

  constructor() { }

  ngOnInit(): void {
    // Initialize filteredOptions with options
    this.filteredOptions = this.options;

    // Subscribe to value changes of control
    this.control.valueChanges.subscribe(value => {
      this.filteredOptions = this.filterOptions(value);
    });
  }

  filterOptions(value: string): string[] {
    // Filter options based on input value
    return this.options.filter(option => option.toLowerCase().includes(value.toLowerCase()));
  }

  showSelect() {
    this.showList = true;
  }

  hideSelect() {
    this.showList = false;
  }

  selectOption(option: string, inputElem: HTMLInputElement) {
    inputElem.value = option;
    this.showList = false;
    this.optionSelected.emit(option);
  }

}
