import { CommonModule } from '@angular/common';
import { ConnectedPosition, Overlay, OverlayModule } from '@angular/cdk/overlay';
import { Component, Input, OnDestroy, TemplateRef } from '@angular/core';
import { MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';

@Component({
  selector: 'app-charge-point-status',
  standalone: true,
  imports: [CommonModule, OverlayModule, MatDialogModule],
  templateUrl: './charge-point-status.component.html',
  styleUrls: ['./charge-point-status.component.sass']
})
export class ChargePointStatusComponent implements OnDestroy {
  @Input() status = 'other';
  legendOpen = false;
  private closeTimer?: ReturnType<typeof setTimeout>;
  private dialogRef?: MatDialogRef<unknown>;
  readonly scrollStrategy = this.overlay.scrollStrategies.reposition();
  readonly positions: ConnectedPosition[] = [
    { originX: 'start', originY: 'bottom', overlayX: 'start', overlayY: 'top', offsetY: 8 },
    { originX: 'end', originY: 'bottom', overlayX: 'end', overlayY: 'top', offsetY: 8 },
    { originX: 'start', originY: 'top', overlayX: 'start', overlayY: 'bottom', offsetY: -8 }
  ];
  readonly statuses = [
    { key: 'available', label: 'Available', color: '#00b050', description: 'The charge point is available for charging.' },
    { key: 'charging', label: 'Charging', color: '#92d050', description: 'A vehicle is charging at the charge point.' },
    { key: 'reserved', label: 'Reserved', color: '#ffc000', description: 'The charge point is reserved for a designated charging session.' },
    { key: 'blocked', label: 'Blocked', color: '#7030a0', description: 'The charge point is blocked and is not available for a new charging session.' },
    { key: 'scheduled', label: 'Scheduled', color: '#00b0f0', description: 'Charging is scheduled to start at a later time.' },
    { key: 'released', label: 'Busy Non Released', color: '#0070c0', description: 'The charging session has finished, but the cable has not been released. A new charging session cannot start yet.' },
    { key: 'busy-non-charging', label: 'Busy Non Charging', color: '#002060', description: 'The charge point is occupied without actively charging. Charging may continue later.' },
    { key: 'maintenance', label: 'Maintenance', color: '#9e9e9e', description: 'The charge point has been placed in maintenance by an owner or operator.' },
    { key: 'error', label: 'Error', color: '#ff0000', description: 'A hardware or software error has been reported by the charge point.' },
    { key: 'disconnected', label: 'Disconnected', color: '#c00000', description: 'A connection to the charge point cannot be established.' },
    { key: 'other', label: 'Other', color: '#808080', description: 'The charge point has a passive or unknown status.' }
  ];

  get currentStatus() {
    const key = (this.status || 'other').toLowerCase().replace(/[ _]+/g, '-');
    return this.statuses.find(item => item.key === key || item.label.toLowerCase().replace(/ /g, '-') === key)
      || this.statuses[this.statuses.length - 1];
  }

  constructor(private overlay: Overlay, private dialog: MatDialog) {}

  showLegend(): void {
    clearTimeout(this.closeTimer);
    if (!this.dialogRef) this.legendOpen = true;
  }

  hideLegend(): void {
    this.closeTimer = setTimeout(() => this.legendOpen = false, 120);
  }

  openDetails(template: TemplateRef<unknown>): void {
    clearTimeout(this.closeTimer);
    this.legendOpen = false;
    if (this.dialogRef) return;
    this.dialogRef = this.dialog.open(template, {
      panelClass: 'vx-status-overlay',
      width: '720px', maxWidth: 'calc(100vw - 32px)', maxHeight: 'calc(100dvh - 32px)',
      ariaLabel: 'Charge point status guide', autoFocus: 'first-tabbable', restoreFocus: true
    });
    this.dialogRef.afterClosed().subscribe(() => {
      this.dialogRef = undefined;
      this.legendOpen = false;
    });
  }

  ngOnDestroy(): void {
    clearTimeout(this.closeTimer);
    this.dialogRef?.close();
  }
}
