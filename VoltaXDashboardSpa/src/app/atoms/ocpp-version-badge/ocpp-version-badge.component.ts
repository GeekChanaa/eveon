import { CommonModule } from '@angular/common';
import { Component, Input, OnChanges, OnDestroy, SimpleChanges } from '@angular/core';
import { Subscription } from 'rxjs';
import { OcppConfigurationService, ocppVersionLabel } from 'src/_services/ocpp-services/ocpp-configuration.service';

/**
 * Small badge with the OCPP version a charger is connected with ("OCPP 1.6" / "OCPP 2.0.1"),
 * or a muted "Offline" when it is not connected.
 * Give it [protocolVersion] when the value is already known, otherwise [chargePointId]
 * (the charger identity, not the DB id) and it asks GET ocpp/configuration/ProtocolVersion itself.
 */
@Component({
  selector: 'app-ocpp-version-badge',
  standalone: true,
  imports: [CommonModule],
  template: `<span *ngIf="loaded" class="ocpp-badge" [class.ocpp-badge_off]="!label"
    [attr.title]="label ? 'Connected with ' + label : 'Not connected: OCPP version unknown'">{{ label || 'Offline' }}</span>`,
  styles: [`
    :host { display: inline-flex; vertical-align: middle; }
    .ocpp-badge {
      display: inline-flex; align-items: center; padding: 3px 10px; border-radius: 999px;
      font-size: 11.5px; font-weight: 700; line-height: 1.4; white-space: nowrap; letter-spacing: .01em;
      background: var(--vx-yellow-soft, rgba(229, 178, 35, .14)); color: var(--vx-yellow-ink, #8A6410);
    }
    .ocpp-badge_off { background: var(--vx-hover, #F4F4F4); color: var(--vx-text-muted, #7A7C80); }
  `]
})
export class OcppVersionBadgeComponent implements OnChanges, OnDestroy {
  @Input() chargePointId?: string | null;
  /** When set (even to null), no request is made. */
  @Input() protocolVersion?: string | null;

  label: string | null = null;
  loaded = false;
  private sub?: Subscription;

  constructor(private _configurationService: OcppConfigurationService) { }

  ngOnChanges(_: SimpleChanges) {
    this.sub?.unsubscribe();
    if (this.protocolVersion !== undefined) {
      this.label = ocppVersionLabel(this.protocolVersion);
      this.loaded = true;
      return;
    }
    if (!this.chargePointId) return;
    this.sub = this._configurationService.getProtocolVersion(this.chargePointId).subscribe({
      next: result => { this.label = ocppVersionLabel(result?.protocolVersion); this.loaded = true; },
      error: () => { this.label = null; this.loaded = true; }
    });
  }

  ngOnDestroy() {
    this.sub?.unsubscribe();
  }
}
