import { Component, forwardRef, Input, OnInit } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
  selector: 'app-enum-select',
  templateUrl: './enum-select.component.html',
  styleUrls: ['./enum-select.component.sass'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => EnumSelectComponent),
      multi: true,
    },
  ],
})
export class EnumSelectComponent<T extends Record<string, string | number>> implements OnInit, ControlValueAccessor  {
  @Input() enumType!: T;
  @Input() placeholder: string = 'Select an option';

  options: { key: string; value: string }[] = [];
  selectedValue!: string;

  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  ngOnInit(): void {
    this.options = Object.entries(this.enumType).map(([key, value]) => ({
      key,
      value: String(value), 
    }));
  }

  writeValue(value: string): void {
    this.selectedValue = value;
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  onSelectionChange(value: string): void {
    this.selectedValue = value;
    this.onChange(value);
    this.onTouched();
  }

}
