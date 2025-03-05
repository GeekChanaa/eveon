import { Component, Input, OnInit } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { AuthService } from 'src/_services/auth.service';
import { UserService } from 'src/_services/user.service';

@Component({
  selector: 'app-user-actions',
  templateUrl: './user-actions.component.html',
  styleUrls: ['./user-actions.component.sass']
})
export class UserActionsComponent implements OnInit {

  @Input() userID : number = 0;
  @Input() userEmail : string = "";

  constructor(
    private _userService:  UserService,
    private _authService : AuthService,
    private _modalService : ActionModalService
  ) { }

  ngOnInit() {
  }

  sendResetPasswordEmail(){
    console.log("this is in here");
    this._authService.resetPasswordRequest(this.userEmail).subscribe((data) => {
      this._modalService.popup(ActionModalStatusEnum.Success, "Success !", "Reset password sent successfully", 4000);
    }, (error) => {
      this._modalService.popup(ActionModalStatusEnum.Error, "Error !", "Something went wrong!", 4000);
    })
  }

  downloadUserInformations(): void {
    this._userService.getUserInformations(this.userID).subscribe((data) => {
      this._userService.downloadUserInformations(data);
    })
  }

}
