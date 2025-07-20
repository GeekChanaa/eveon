import { Component, EventEmitter, forwardRef, Input, OnInit, Output } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
  selector: 'app-toggle',
  templateUrl: './toggle.component.html',
  styleUrls: ['./toggle.component.sass'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => ToggleComponent),
      multi: true,
    },
  ],
})
export class ToggleComponent implements OnInit, ControlValueAccessor  {

  @Input() isToggled: boolean = false;
  @Output() isToggledChange: EventEmitter<boolean> = new EventEmitter<boolean>();
  private onChange: (value: boolean) => void = () => {};
  @Output() toggled: EventEmitter<boolean> = new EventEmitter<boolean>();
  private onTouched: () => void = () => {};
    @Input() togglable: boolean = true;

  toggle() {
    if (!this.togglable) return;
    
    this.isToggled = !this.isToggled;
    this.isToggledChange.emit(this.isToggled);
    this.toggled.emit(this.isToggled);
    this.onChange(this.isToggled);
    this.onTouched();
  }

  constructor() { }
  
  writeValue(value: boolean): void {
    this.isToggled = value;
  }

  registerOnChange(fn: (value: boolean) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  ngOnInit() {
  }

}
