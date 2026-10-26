import { Component, Input } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { AccessService } from 'src/_services/access.service';
import { ActionModalService } from 'src/_services/action-modal.service';
import { PartnerService, PartnerUserCreated } from 'src/_services/partner.service';

/**
 * Creates the account a partner signs in with (role Partner, on the partner's email). Administrators only; the
 * generated password is emailed to the administrators, never shown here.
 */
@Component({
  selector: 'app-partner-account',
  template: `
    <section *ngIf="isAdmin" class="partner-account" aria-labelledby="partner-account-title">
      <div class="partner-account__copy">
        <h3 id="partner-account-title">Partner account</h3>
        <p *ngIf="!created">
          Create the account this partner signs in with, on {{ email || "the partner's email" }}, with the Partner role.
          Its password is emailed to the administrators.
        </p>
        <p *ngIf="created" role="status">
          Account created for <strong>{{ created.email }}</strong>. Its password was emailed to
          {{ created.recipients }} administrator{{ created.recipients === 1 ? '' : 's' }}.
        </p>
      </div>
      <app-button *ngIf="!created" [variant]="'secondary'" [isLoading]="creating" [disabled]="creating || !partnerId"
        (btnClick)="create()">Create partner account</app-button>
    </section>`,
  styles: [`
    .partner-account { display: flex; flex-wrap: wrap; align-items: center; justify-content: space-between; gap: 12px 24px;
      margin: 0 0 20px; padding: 16px 20px; border: 1px solid var(--vx-border); border-radius: var(--vx-radius, 10px);
      background: var(--vx-surface); color: var(--vx-text); }
    .partner-account__copy { flex: 1 1 320px; }
    h3 { margin: 0 0 4px; font-size: 16px; }
    p { margin: 0; font-size: 14px; line-height: 1.5; color: var(--vx-text-muted); }
    strong { color: var(--vx-text); }
  `]
})
export class PartnerAccountComponent {
  @Input() partnerId = 0;
  @Input() email?: string;

  creating = false;
  created: PartnerUserCreated | null = null;

  constructor(
    private _access: AccessService,
    private _partnerService: PartnerService,
    private _modalService: ActionModalService
  ) {}

  get isAdmin(): boolean { return this._access.isAdmin; }

  create(): void {
    this.creating = true;
    this._partnerService.createPartnerUser(this.partnerId).subscribe({
      next: (created) => {
        this.creating = false;
        this.created = created;
        this._modalService.popup(ActionModalStatusEnum.Success, 'Partner account created', `The password of ${created.email} was emailed to the administrators.`, 4000);
      },
      error: (error) => {
        this.creating = false;
        this._modalService.popup(ActionModalStatusEnum.Error, 'Error', error?.error?.error || 'The partner account could not be created.', 5000);
      }
    });
  }
}
