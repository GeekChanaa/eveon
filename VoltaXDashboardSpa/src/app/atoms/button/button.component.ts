import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';

@Component({
  selector: 'app-button',
  templateUrl: './button.component.html',
  styleUrls: ['./button.component.sass']
})
export class ButtonComponent{

  @Input() size: 'small' | 'standard' | 'large' = 'standard';
  @Input() type: 'button' | 'submit' | 'reset' = 'button';
  @Input() variant: 'primary' | 'secondary' | 'professional' | 'danger' = 'primary';
  @Input() outlined = false;
  @Input() isLoading = false;
  @Input() disabled = false;
  @Input() fullWidth = false;
  @Input() ariaLabel?: string;

  @Output() btnClick = new EventEmitter<MouseEvent>();
  @Output() btnFocus = new EventEmitter<FocusEvent>();
  @Output() btnBlur = new EventEmitter<FocusEvent>();
  @Output() btnKeydown = new EventEmitter<KeyboardEvent>();
  @Output() btnKeyup = new EventEmitter<KeyboardEvent>();
  @Output() btnMouseenter = new EventEmitter<MouseEvent>();
  @Output() btnMouseleave = new EventEmitter<MouseEvent>();

  onClick(event: MouseEvent): void {
    this.btnClick.emit(event);
  }

  onFocus(event: FocusEvent): void {
    this.btnFocus.emit(event);
  }

  onBlur(event: FocusEvent): void {
    this.btnBlur.emit(event);
  }

  onKeyDown(event: KeyboardEvent): void {
    this.btnKeydown.emit(event);
  }

  onKeyUp(event: KeyboardEvent): void {
    this.btnKeyup.emit(event);
  }

  onMouseEnter(event: MouseEvent): void {
    this.btnMouseenter.emit(event);
  }

  onMouseLeave(event: MouseEvent): void {
    this.btnMouseleave.emit(event);
  }

  get buttonClasses(): string {
    return `
      btn
      btn-${this.size}
      btn-${this.variant}
      ${this.outlined ? 'btn-outlined' : ''}
      ${this.fullWidth ? 'btn-full-width' : ''}
    `;
  }

}
