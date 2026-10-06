import { Component, Input, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { AuthService } from 'src/_services/auth.service';
import { MeService } from 'src/_services/me.service';
import { UserInfoDownloadRequestService } from 'src/_services/user-info-download-request.service';

@Component({
  selector: 'app-profile-privacy',
  templateUrl: './profile-privacy.component.html',
  styleUrls: ['./profile-privacy.component.sass']
})
export class ProfilePrivacyComponent implements OnInit {
  @Input() userID: number = 0;

  lastRequest: any = null;
  loadingRequest = false;
  requesting = false;
  exportToken: string | null = null;
  downloading = false;
  downloadError = '';

  showDeleteModal = false;
  deletePassword = '';
  deleteConfirmed = false;
  deleting = false;
  deleteError = '';

  constructor(
    private route: ActivatedRoute,
    private requests: UserInfoDownloadRequestService,
    private me: MeService,
    private auth: AuthService,
    private modal: ActionModalService
  ) {}

  ngOnInit() {
    this.exportToken = this.route.snapshot.queryParamMap.get('export');
    this.loadLastRequest();
  }

  get isExpired(): boolean {
    return !!this.lastRequest?.expiresAt && new Date(this.lastRequest.expiresAt).getTime() < Date.now();
  }

  get readyToDownload(): boolean {
    return this.lastRequest?.status === 'Completed' && !this.lastRequest.downloaded && !this.isExpired;
  }

  get canRequestExport(): boolean {
    if (!this.lastRequest) return true;
    return ['Denied', 'Expired', 'Failed', 'Completed'].includes(this.lastRequest.status) && !this.readyToDownload;
  }

  loadLastRequest() {
    if (!this.userID) return;
    this.loadingRequest = true;
    this.requests.userLastRequest(this.userID).subscribe({
      next: data => { this.lastRequest = data; this.loadingRequest = false; },
      error: () => { this.lastRequest = null; this.loadingRequest = false; }
    });
  }

  requestExport() {
    this.requesting = true;
    this.requests.createRequest(this.userID).subscribe({
      next: () => {
        this.requesting = false;
        this.modal.popup(ActionModalStatusEnum.Success, 'Request sent', 'We will email you when your data is ready.', 4000);
        this.loadLastRequest();
      },
      error: () => {
        this.requesting = false;
        this.modal.popup(ActionModalStatusEnum.Error, 'Error', 'Could not send the request. Please try again later.', 4000);
      }
    });
  }

  download() {
    if (!this.exportToken) return;
    this.downloading = true;
    this.downloadError = '';
    this.me.downloadDataExport(this.exportToken).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = 'eveon-my-data.zip';
        link.click();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
        this.downloading = false;
        this.exportToken = null;
        this.loadLastRequest();
      },
      error: () => {
        this.downloading = false;
        this.downloadError = 'This download link is invalid, already used or expired.';
      }
    });
  }

  openDelete() {
    this.deletePassword = '';
    this.deleteConfirmed = false;
    this.deleteError = '';
    this.showDeleteModal = true;
  }

  closeDelete() {
    if (!this.deleting) this.showDeleteModal = false;
  }

  confirmDelete() {
    if (!this.deleteConfirmed) return;
    this.deleting = true;
    this.deleteError = '';
    this.me.deleteAccount(this.deletePassword || null).subscribe({
      next: () => {
        this.deleting = false;
        this.showDeleteModal = false;
        // Clears the local session and navigates to /goodbye.
        this.auth.logout();
      },
      error: error => {
        this.deleting = false;
        this.deleteError = error?.error?.error ?? 'Your account could not be deleted. Please try again.';
      }
    });
  }
}
