import { Directive, HostListener, Input } from '@angular/core';
import { ConfirmService } from 'src/_services/confirm.service';

/**
 * Asks for confirmation before running <c>confirmCallback</c> on click. Title, message and button label are
 * translation keys (with <c>confirmParams</c>), e.g.
 * <button appConfirmAction confirmTitle="operator.pricing.resetTitle" confirmMessage="operator.pricing.resetText" [confirmCallback]="reset.bind(this)">
 */
@Directive({
  selector: '[appConfirmAction]'
})
export class ConfirmActionDirective {
  @Input() confirmTitle: string = 'operator.confirm.title';
  @Input() confirmMessage: string = 'operator.confirm.message';
  @Input() confirmLabel?: string;
  @Input() confirmTone: 'default' | 'danger' = 'default';
  @Input() confirmParams?: object;
  @Input() confirmCallback!: (...args: any[]) => any;
  @Input() confirmArgs: any[] = [];

  constructor(private confirmService: ConfirmService) {}

  @HostListener('click', ['$event'])
  handleClick(event: MouseEvent): void {
    event.preventDefault();
    this.confirmService.ask({
      title: this.confirmTitle, message: this.confirmMessage, params: this.confirmParams, confirmLabel: this.confirmLabel, tone: this.confirmTone
    }).subscribe(() => this.confirmCallback?.(...this.confirmArgs));
  }
}
