import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';

export type CardActionSize = 'small' | 'standard' | 'large';

@Component({
  selector: 'app-card-action',
  templateUrl: './card-action.component.html',
  styleUrls: ['./card-action.component.sass']
})
export class CardActionComponent implements OnInit  {
  ngOnInit(): void {
    console.log(this.actionName)
  }
  
  @Input() icon: string = 'info';
  @Input() actionName: string = 'Default Action';
  @Input() description?: string;
  @Input() disabled: boolean = false;
  @Input() size: CardActionSize = 'standard';
  @Output() actionClick = new EventEmitter<void>();

  handleClick(): void {
    if (!this.disabled) {
      this.actionClick.emit();
    }
  }
}
