import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';

@Component({
  selector: 'app-toggle',
  templateUrl: './toggle.component.html',
  styleUrls: ['./toggle.component.sass']
})
export class ToggleComponent implements OnInit {

  @Input() isToggled: boolean = false;
  @Output() isToggledChange: EventEmitter<boolean> = new EventEmitter<boolean>();
  
  @Output() toggled: EventEmitter<boolean> = new EventEmitter<boolean>();

  toggle() {
    this.isToggled = !this.isToggled;
    this.isToggledChange.emit(this.isToggled);
    this.toggled.emit(this.isToggled);
  }

  constructor() { }

  ngOnInit() {
  }

}
