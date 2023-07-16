import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { UserRole } from 'src/_models/_enums/user-role';
import { AuthService } from 'src/_services/auth.service';
import { UserService } from 'src/_services/user.service';
@Component({
  selector: 'app-index',
  templateUrl: './index.component.html',
  styleUrls: ['./index.component.css']
})
export class IndexComponent implements OnInit {

  constructor(
    private _userService : UserService,
    private _authService : AuthService,
    private _router : Router
  ) { }

  ngOnInit() {
    var userID = parseInt(this._authService.getAuthInformation().nameid);
    this._userService.getById(userID).subscribe((u) => {
      if(u.role == UserRole.Admin)
      this._router.navigate(['/dashboard']);
      else if(u.role == UserRole.Customer)
      this._router.navigate(['/my-dashboard']);
      else if(u.role == UserRole.Partner)
      this._router.navigate(['/partner-dashboard']);
    });
  }

}
