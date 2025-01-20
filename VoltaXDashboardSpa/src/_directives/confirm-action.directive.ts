import { Directive, EventEmitter, HostListener, Input, Output } from '@angular/core';
import { ConfirmService } from 'src/_services/confirm.service';

@Directive({
  selector: '[appConfirmAction]'
})
export class ConfirmActionDirective {
  @Input() confirmTitle: string = 'Confirm Action';
  @Input() confirmMessage: string = 'Are you sure you want to proceed?';
  @Input() confirmCallback!: (...args: any[]) => any;
  @Input() confirmArgs: any[] = []; 

  constructor(private confirmService: ConfirmService) {}

  @HostListener('click', ['$event'])
  handleClick(event: MouseEvent): void {
    event.preventDefault(); 
    this.confirmService.requestConfirmation(this.confirmTitle, this.confirmMessage, () => {
      if (this.confirmCallback) {
        this.confirmCallback(...this.confirmArgs); 
      }
    });
  }
}
