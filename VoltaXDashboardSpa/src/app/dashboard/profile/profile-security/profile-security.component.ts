import { Component, OnInit, Input } from '@angular/core';
import { UserPasswordChangeDto } from 'src/_models/_dtos/user-password-change-dto';
import { AuthService } from 'src/_services/auth.service';

@Component({
  selector: 'app-profile-security',
  templateUrl: './profile-security.component.html',
  styleUrls: ['./profile-security.component.css']
})
export class ProfileSecurityComponent implements OnInit {

  editingPassword : boolean = false;
  @Input() userID : number = 0;
  userPasswordChange : UserPasswordChangeDto = {
    id: this.userID,
    currentPassword: '',
    newPassword: '',
    newPasswordCheck: ''
  };
  

  constructor(
    private _authService : AuthService
  ) { }

  ngOnInit() {
  }

  changePassword(){
    this.userPasswordChange.id = this.userID;
    this._authService.changePassword(this.userPasswordChange).subscribe((data) => {
      this.editingPassword = false;
    })
  }

}
