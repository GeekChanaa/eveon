import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { AuthService } from 'src/_services/auth.service';
import { AccessService } from 'src/_services/access.service';
import { SidebarService } from 'src/_services/sidebar.service';
import { UserService } from 'src/_services/user.service';
import { avatarUrl } from 'src/_helpers/avatar-url';
@Component({ selector: 'app-navbar', templateUrl: './navbar.component.html', styleUrls: ['./navbar.component.sass'] })
export class NavbarComponent implements OnInit, OnDestroy {
  user: any = {};
  imageUrl: string | null = null;
  userInitials = '?';
  private subscriptions = new Subscription();
  constructor(private auth: AuthService, public access: AccessService, private router: Router,
    private users: UserService, private sidebar: SidebarService) {}
  ngOnInit(): void {
    this.subscriptions.add(this.access.load().subscribe({ next: info => {
      this.user = info;
      this.imageUrl = avatarUrl(info.imageUrl);
      this.userInitials = ((info.firstName?.charAt(0) || '') + (info.lastName?.charAt(0) || '')).toUpperCase() || '?';
    }, error: () => {} }));
    this.subscriptions.add(this.users.getAvatarUrl().subscribe(url => { if (url) this.imageUrl = avatarUrl(url); }));
  }
  toggleSidebar(): void { this.sidebar.toggleOpened(); }
  logout(): void { this.auth.logout(); }
  goToProfile(): void {
    this.router.navigateByUrl(this.access.can('AccessDashboard') ? '/dashboard/profile' : this.user.role === 'Partner' && this.user.partnerId ? '/partner-dashboard/profile' : '/my-dashboard/profile');
  }
  onImageError(): void { this.imageUrl = null; }
  ngOnDestroy(): void { this.subscriptions.unsubscribe(); }
}
