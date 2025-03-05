import { Component, ElementRef, HostListener, OnInit } from '@angular/core';
import { ActivatedRouteSnapshot, RouterStateSnapshot, Router } from '@angular/router';
import { UserRole } from 'src/_models/_enums/user-role';
import { AuthService } from 'src/_services/auth.service';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.sass']
})
export class NavbarComponent implements OnInit {

  constructor(
    private _authService : AuthService,
    private _router: Router,
    private _eRef: ElementRef
  ) { }

  avatarMenuBody : boolean = false;

  // On init cycle hook
  ngOnInit() {
  }

  // Logout
  logout(){
    this._authService.logout();
  }

  toggleUserMenu(){
    this.avatarMenuBody = this.avatarMenuBody ? false : true;
  }

  @HostListener('document:click', ['$event'])
  onClickOutside(event: Event) {
    if (this.avatarMenuBody && !this._eRef.nativeElement.querySelector('.header__item_user')?.contains(event.target as Node)) {
      this.avatarMenuBody = false;
    }
  }

  @HostListener('document:keydown.escape')
  onEscPress() {
    if (this.avatarMenuBody) {
      this.avatarMenuBody = false;
    }
  }

  goToProfile(){
    var role = this._authService.getRole();
    console.log("this is the role", role);
    if(role == "Admin")
      this._router.navigate(['/dashboard/profile'])
    else if(role == "Customer")
      this._router.navigate(['/my-dashboard/profile'])
    else if(role == "Partner")
      this._router.navigate(['/partner-dashboard/profile'])

    this.avatarMenuBody = false;
  }
}
