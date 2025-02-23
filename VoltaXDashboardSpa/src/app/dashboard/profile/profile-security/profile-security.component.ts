import { Component, OnInit, Input } from '@angular/core';
import { UserPasswordChangeDto } from 'src/_models/_dtos/user-password-change-dto';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { AuthService } from 'src/_services/auth.service';

@Component({
  selector: 'app-profile-security',
  templateUrl: './profile-security.component.html',
  styleUrls: ['./profile-security.component.sass']
})
export class ProfileSecurityComponent implements OnInit {

  editingPassword : boolean = false;
  isLoading : boolean = false;
  @Input() userID : number = 0;

  userPasswordChange : UserPasswordChangeDto = {
    id: this.userID,
    currentPassword: '',
    newPassword: '',
    newPasswordCheck: ''
  };
  

  constructor(
    private _authService : AuthService,
    private _modalService : ActionModalService
  ) { }

  ngOnInit() {
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

}
