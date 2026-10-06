import { Component, OnInit, Input } from '@angular/core';
import { UserPasswordChangeDto } from 'src/_models/_dtos/user-password-change-dto';
import { LinkedAccountsDto } from 'src/_models/_dtos/linked-accounts-dto';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { AuthService } from 'src/_services/auth.service';
import { GeolocationService } from 'src/_services/geolocation.service';

@Component({
  selector: 'app-profile-security',
  templateUrl: './profile-security.component.html',
  styleUrls: ['./profile-security.component.sass']
})
export class ProfileSecurityComponent implements OnInit {

  editingPassword : boolean = false;
  isLoading : boolean = false;
  @Input() userID : number = 0;

  linkedAccounts : LinkedAccountsDto | null = null;
  linkedAccountsError : string = "";
  isUnlinkingGoogle : boolean = false;
  isSigningOutEverywhere = false;
  signOutError = '';

  constructor(
    private _authService : AuthService,
    private _modalService : ActionModalService,
    private _geolocation: GeolocationService
  ) { }

  get locationConsent() {
    return this._geolocation.consent$.value;
  }

  grantLocationConsent() {
    this._geolocation.grantConsent();
    this._geolocation.requestLocation().catch(() => { });
  }

  revokeLocationConsent() {
    this._geolocation.revokeConsent();
  }

  ngOnInit() {
    this.getLinkedAccounts();
  }

  signOutEverywhere() {
    this.isSigningOutEverywhere = true;
    this.signOutError = '';
    this._authService.logoutEverywhere().subscribe({
      error: () => {
        this.isSigningOutEverywhere = false;
        this.signOutError = 'Could not sign out all devices. Please try again.';
      }
    });
  }

  getLinkedAccounts(){
    this._authService.getLinkedAccounts().subscribe((data) => {
      this.linkedAccounts = data;
    },(error) => {
      this.linkedAccountsError = "Could not load your connected accounts.";
    });
  }

  // Leaves the SPA, comes back on /auth/google-callback?linked=true
  linkGoogle(){
    this._authService.startGoogleLink('/dashboard/profile');
  }

  unlinkGoogle(){
    this.isUnlinkingGoogle = true;
    this.linkedAccountsError = "";
    this._authService.unlinkGoogle().subscribe((data) => {
      this.isUnlinkingGoogle = false;
      this.linkedAccounts = data;
      this._modalService.popup(ActionModalStatusEnum.Success,"Success !","Google account disconnected", 4000);
    },(error) => {
      this.isUnlinkingGoogle = false;
      this.linkedAccountsError = error?.error?.error ?? "Could not disconnect your Google account.";
    });
  }

}
