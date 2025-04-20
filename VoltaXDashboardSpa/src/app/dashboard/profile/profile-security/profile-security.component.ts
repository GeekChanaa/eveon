import { Component, OnInit, Input } from '@angular/core';
import { UserPasswordChangeDto } from 'src/_models/_dtos/user-password-change-dto';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { AuthService } from 'src/_services/auth.service';
import { UserInfoDownloadRequestService } from 'src/_services/user-info-download-request.service';
import { UserService } from 'src/_services/user.service';

@Component({
  selector: 'app-profile-security',
  templateUrl: './profile-security.component.html',
  styleUrls: ['./profile-security.component.sass']
})
export class ProfileSecurityComponent implements OnInit {

  editingPassword : boolean = false;
  isLoading : boolean = false;
  @Input() userID : number = 0;

  showRequestConfirmModal : boolean = false;

  isLoadingRequest : boolean = false;

  lastRequest : any | null = null;

  userPasswordChange : UserPasswordChangeDto = {
    id: this.userID,
    currentPassword: '',
    newPassword: '',
    newPasswordCheck: ''
  };
  

  constructor(
    private _authService : AuthService,
    private _userService: UserService,
    private _modalService : ActionModalService,
    private _userInfoDownloadRequestService: UserInfoDownloadRequestService
  ) { }

  ngOnInit() {
    this.getLastUserRequest();
  }

  changePassword(){
    this.isLoading = true;
    this.userPasswordChange.id = this.userID;
    this._authService.changePassword(this.userPasswordChange).subscribe((data) => {
      this._modalService.popup(ActionModalStatusEnum.Success,"Success !","Password Changed Successfully ! ", 4000);
      this.isLoading = false
      this.editingPassword = false;
    },(error) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Error,"Error","Something Went wrong please try again later ! ", 4000);
    })
  }

  downloadUserInformations(){
    this._userService.getUserInformations(this.userID).subscribe((data) => {
      this._userService.downloadUserInformations(data);
    })
  }

  confirmDelete(itemId: any): void {
    this.showRequestConfirmModal = true;
  }

  cancelRequest(): void {
    this.showRequestConfirmModal = false;
  }

  proceedWithRequest(): void {
    this._userInfoDownloadRequestService.createRequest(this.userID).subscribe((data) => {
      this._modalService.popup(ActionModalStatusEnum.Success,"Success !","Request Sent! ", 4000);
      this.showRequestConfirmModal = false;
    },(error) => {
      this._modalService.popup(ActionModalStatusEnum.Error,"Error","Something Went wrong please try again later ! ", 4000);
      this.showRequestConfirmModal = false;
    })
  }

  getLastUserRequest(){
    this.isLoadingRequest = true;
    this._userInfoDownloadRequestService.userLastRequest(this.userID).subscribe((data) => {
      console.log(data);
      this.isLoadingRequest = false;
      this.lastRequest = data;
    })
  }

}
