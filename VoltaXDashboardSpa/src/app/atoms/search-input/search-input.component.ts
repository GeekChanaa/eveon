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
  @Output() ngModelChange = new EventEmitter<string>();
  @Input() placeholder : string = "Value"
  @Input() disabled : boolean = false;

  searchString: string = "";

  // Add a new property for filtered options
  filteredOptions: any[] = [];


  showList = false;

  constructor() { }

  ngOnInit(): void {
    console.log(this.options);
    // Initialize filteredOptions with options
    this.filteredOptions = this.options;

    // Subscribe to value changes of control
    // this.control.valueChanges.subscribe(value => {
    //   this.filteredOptions = this.filterOptions(value);
    //   this.ngModelChange.emit(value);
    // });
  }

  filterOptions() {
    console.log("here ", this.searchString);
    this.filteredOptions =  this.options.filter(option => option.name.toLowerCase().includes(this.searchString.toLowerCase()));
  }

  showSelect() {
    this.showList = true;
  }

  hideSelect() {
    this.showList = false;
  }

  selectOption(option: any, inputElem: HTMLInputElement) {
    inputElem.value = option.id + "-" + option.name;
    this.control.setValue(option.id);
    this.showList = false;
    this.optionSelected.emit(option);
  }

}
