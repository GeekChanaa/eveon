import { Component, ElementRef, HostListener, OnInit } from '@angular/core';
import { ActivatedRouteSnapshot, RouterStateSnapshot, Router } from '@angular/router';
import { UserRole } from 'src/_models/_enums/user-role';
import { AuthService } from 'src/_services/auth.service';
import { UserService } from 'src/_services/user.service';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.sass']
})
export class NavbarComponent implements OnInit {
  staticUrl : string = environment.apiStaticFilesUrl;
  user : any = {};
  imageUrl : string | null = null;
  userInitials: string = "";

  constructor(
    private _authService : AuthService,
    private _router: Router,
    private _eRef: ElementRef,
    private _userService : UserService
  ) { }

  avatarMenuBody : boolean = false;

  // On init cycle hook
  ngOnInit() {
    var decodedToken = this._authService.getAuthInformation();
    let userID = parseInt(decodedToken.nameid);
    this.getUserByID(userID);
    this._userService.getAvatarUrl().subscribe((url) => {
      if(url != null)
        this.imageUrl = this.staticUrl+url;
    });
  }

  getUserByID(id : number ){
    this._userService.getUserInformations(id).subscribe((data) => {
      this.user = data;

      // set avatar URL
      this.imageUrl = this.user.imageUrl || null;

      // set initials
      this.setUserInitials();
    })
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

  onImageError() {
    this.imageUrl = null; // fallback to initials
  }

  setUserInitials() {
    if (!this.user?.firstName && !this.user?.lastName) {
      this.userInitials = "?";
      return;
    }

    const first = this.user?.firstName?.charAt(0) || "";
    const last = this.user?.lastName?.charAt(0) || "";
    this.userInitials = (first + last).toUpperCase();
  }

}
