import { Component, HostListener, OnInit } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from '@angular/router';
import { AuthService } from 'src/_services/auth.service';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css']
})
export class NavbarComponent implements OnInit {

  constructor(
    private _authService : AuthService,
    private _router: Router
  ) { }

  avatarMenuBody : boolean = false;

  // On init cycle hook
  ngOnInit() {
  }

  // Logout
  logout(){
    this._authService.logout();
    this._router.navigate(['/auth/login']);
  }

  // header avatar 
  header_avatar(){
    this.avatarMenuBody = this.avatarMenuBody ? false : true;
  }
}
