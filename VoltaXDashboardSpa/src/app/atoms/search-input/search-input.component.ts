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
  showList = false;

  constructor() { }

  ngOnInit(): void {
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
