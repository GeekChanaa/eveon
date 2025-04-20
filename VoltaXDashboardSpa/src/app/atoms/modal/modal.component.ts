import { trigger, transition, style, animate } from '@angular/animations';
import { Component, ElementRef, EventEmitter, Input, OnInit, Output } from '@angular/core';

@Component({
  selector: 'app-modal',
  templateUrl: './modal.component.html',
  styleUrls: ['./modal.component.sass'],
  animations: [
    trigger('backdropAnimation', [
      transition('void => open', [
        style({ opacity: 0 }),
        animate('200ms ease-out', style({ opacity: 1 }))
      ]),
      transition('open => void', [
        animate('200ms ease-in', style({ opacity: 0 }))
      ])
    ]),
    trigger('modalAnimation', [
      transition('void => open', [
        style({ opacity: 0, transform: 'scale(0.8)' }),
        animate('300ms ease-out', style({ opacity: 1, transform: 'scale(1)' }))
      ]),
      transition('open => void', [
        animate('200ms ease-in', style({ opacity: 0, transform: 'scale(0.8)' }))
      ])
    ])
  ]
})
export class ModalComponent implements OnInit {
  @Input() title: string = 'Modal Title';
  @Input() modalSize: 'small' | 'medium' | 'large' = 'medium';
  @Input() showFooter: boolean = true;
  @Input() showCancelButton: boolean = true;
  @Input() showConfirmButton: boolean = true;
  @Input() cancelText: string = 'Cancel';
  @Input() confirmText: string = 'Confirm';
  @Input() closeOnOutsideClick: boolean = true;

  @Output() closed = new EventEmitter<void>();
  @Output() canceled = new EventEmitter<void>();
  @Output() confirmed = new EventEmitter<void>();

  modalState: 'void' | 'open' = 'open';

  constructor(private elementRef: ElementRef) {}
  ngOnInit(): void {
    throw new Error('Method not implemented.');
  }

  close(): void {
    this.modalState = 'void';
    setTimeout(() => {
      this.closed.emit();
    }, 200);
  }

  cancel(): void {
    this.canceled.emit();
    this.close();
  }

  confirm(): void {
    this.confirmed.emit();
    this.close();
  }

  closeOnBackdrop(event: MouseEvent): void {
    if (this.closeOnOutsideClick && event.target === event.currentTarget) {
      this.close();
    }
  }

}
