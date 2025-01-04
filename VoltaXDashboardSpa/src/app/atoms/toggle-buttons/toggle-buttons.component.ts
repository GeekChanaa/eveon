import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';

@Component({
  selector: 'app-toggle-buttons',
  templateUrl: './toggle-buttons.component.html',
  styleUrls: ['./toggle-buttons.component.sass']
})
export class ToggleButtonsComponent implements OnInit {
  @Input() optionOne: string = 'Option 1';
  @Input() optionTwo: string = 'Option 2';
  @Input() selectedOption: string = this.optionOne;

  @Output() selectionChange: EventEmitter<string> = new EventEmitter<string>();

  toggle(option: string): void {
    this.selectedOption = option;
    this.selectionChange.emit(this.selectedOption);
  }
  constructor() { }

  ngOnInit() {
    
  }

}
