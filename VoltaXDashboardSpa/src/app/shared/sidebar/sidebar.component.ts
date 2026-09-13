import { AfterViewInit, Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { UserRole } from 'src/_models/_enums/user-role';
import { AuthService } from 'src/_services/auth.service';
import { EnumMappingService } from 'src/_services/enum-mapping.service';
import { PartnerService } from 'src/_services/partner.service';
import { SidebarService } from 'src/_services/sidebar.service';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-sidebar',
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.sass']
})
export class SidebarComponent implements OnInit, AfterViewInit, OnDestroy {


  @ViewChild('sidebar') sidebar!: ElementRef;
  @ViewChild('close') close!: ElementRef;
  @ViewChild('helpOpen') helpOpen!: ElementRef;
  @ViewChild('help') help!: ElementRef;
  @ViewChild('helpOverlay') helpOverlay!: ElementRef;
  @ViewChild('helpClose') helpClose!: ElementRef;

  theme : any = {};

  checked : boolean = false;

  partnerID : number = 0;

  partnerImageUrl : string = "";
  
  staticUrl : string = environment.apiStaticFilesUrl;
  

  // User Role
  role : UserRole = UserRole.Customer;

  currentUrl: string = "";

  // Rail mode (desktop) and drawer state (mobile), owned by SidebarService.
  collapsed : boolean = false;
  opened : boolean = false;

  private subscriptions = new Subscription();

  constructor(
    private router: Router,
    private _authService : AuthService,
    private _partnerService : PartnerService,
    public _enumMappingService : EnumMappingService,
    private _sidebarService : SidebarService
  ) { }

  ngOnInit() {
    this.subscriptions.add(
      this._sidebarService.collapsed$.subscribe(value => this.collapsed = value)
    );
    this.subscriptions.add(
      this._sidebarService.opened$.subscribe(value => this.opened = value)
    );
    this.currentUrl = this.router.url;
    this.subscriptions.add(
      this.router.events.subscribe(event => {
        this.currentUrl = this.router.url;
        // A navigation on mobile means the user picked a link — close the drawer.
        if (event instanceof NavigationEnd) this._sidebarService.close();
      })
    );

    this.role = this._authService.getRole();
    let userDecodedToken = this._authService.decodedToken;
    if(this._authService.decodedToken.partnerID != null && this._authService.decodedToken.partnerID != undefined){
      this.partnerID = parseInt(this._authService.decodedToken.partnerID);
      this.getPartnerImage();
    }
    var mode = localStorage.getItem("darkMode");
    if(mode == "on")
      this.checked = true;
  }

  ngAfterViewInit() {
    this.bindEvents();
  }

  ngOnDestroy() {
    this.subscriptions.unsubscribe();
  }

  bindEvents() {
    this.close.nativeElement.addEventListener('click', () => this.closeDrawer());
    this.helpOverlay.nativeElement.addEventListener('click', () => this.hideHelp());
    this.helpClose.nativeElement.addEventListener('click', () => this.hideHelp());
  }

  // Icon toggle: collapses to an icon rail on desktop, opens the drawer on mobile.
  toggleCollapsed() {
    this._sidebarService.toggleCollapsed();
  }

  closeDrawer() {
    this._sidebarService.close();
  }

  showHelp() {
    this.help.nativeElement.classList.add('active');
    this.helpOverlay.nativeElement.classList.add('active');
  }

  hideHelp() {
    this.help.nativeElement.classList.remove('active');
    this.helpOverlay.nativeElement.classList.remove('active');
  }

  /**
   * True when the current route is <paramref name="link"/> or sits below it.
   *
   * Compared segment by segment on purpose: a plain startsWith lights up
   * "/dashboard/charging-stations" while standing on
   * "/dashboard/charging-stations-archive".
   */
  isActive(link: string): boolean {
    const current = this.normalize(this.currentUrl);
    const target = this.normalize(link);

    return current === target || current.startsWith(target + '/');
  }

  /** Home links have children everywhere, so they only light up on an exact match. */
  isExactActive(link: string): boolean {
    return this.normalize(this.currentUrl) === this.normalize(link);
  }

  /** A group is active when any of the routes it holds is. */
  isDropdownActive(...baseUrls: string[]): boolean {
    return baseUrls.some(url => this.isActive(url));
  }

  /** Drops the query string, the fragment and any trailing slash. */
  private normalize(url: string): string {
    if (url == null) return '';

    let value = url.split('?')[0].split('#')[0];
    while (value.length > 1 && value.endsWith('/')) {
      value = value.substring(0, value.length - 1);
    }

    return value;
  }

  getPartnerImage(){
    this._partnerService.getPartnerLogoUrl(this.partnerID).subscribe((data) => {
      this.partnerImageUrl = this.staticUrl + data.url;
    })
  }

  onThemeToggle(){
     localStorage.setItem('darkMode', this.checked ? 'on' : 'off');
     document.body.classList.toggle('dark', this.checked);
  }

  

}
