import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormControl } from '@angular/forms';

@Component({
  selector: 'app-search-input',
  templateUrl: './search-input.component.html',
  styleUrls: ['./search-input.component.sass']
})
export class SearchInputComponent implements OnInit {

  @Input() options: any[] = [];
  @Input() control: FormControl = new FormControl('');
  @Output() optionSelected = new EventEmitter<any>();

  // Add a new property for filtered options
  filteredOptions: any[] = [];


  showList = false;

  constructor() { }

  ngOnInit(): void {
    // Initialize filteredOptions with options
    this.filteredOptions = this.options;

    console.log("this is the options in the beginning");
    console.log(this.options);

    // Subscribe to value changes of control
    this.control.valueChanges.subscribe(value => {
      this.filteredOptions = this.filterOptions(value);
    });
  }

  filterOptions(value: string): string[] {

    // Filter options based on input value
    return this.options.filter(option => option.name.toLowerCase().includes(value.toLowerCase()));
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
    console.log(option);
    this.optionSelected.emit(option);
  }

}
