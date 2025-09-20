import { AfterViewInit, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { UserRole } from 'src/_models/_enums/user-role';
import { AuthService } from 'src/_services/auth.service';
import { EnumMappingService } from 'src/_services/enum-mapping.service';
import { PartnerService } from 'src/_services/partner.service';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-sidebar',
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.sass']
})
export class SidebarComponent implements OnInit, AfterViewInit {


  @ViewChild('sidebar') sidebar!: ElementRef;
  // @ViewChild('toggle') toggle!: ElementRef;
  @ViewChild('overlay') overlay!: ElementRef;
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

  constructor(
    private router: Router,
    private _authService : AuthService,
    private _partnerService : PartnerService,
    public _enumMappingService : EnumMappingService
  ) { }

  ngOnInit() {
    this.role = this._authService.getRole();
    let userDecodedToken = this._authService.decodedToken;
    if(this._authService.decodedToken.partnerID != null && this._authService.decodedToken.partnerID != undefined){
      this.partnerID = parseInt(this._authService.decodedToken.partnerID);
      this.getPartnerImage();
    }
    var mode = localStorage.getItem("darkMode");
    if(mode == "on")
      this.checked = true;
    this.router.events.subscribe(() => {
      this.currentUrl = this.router.url;
    });
  }

  ngAfterViewInit() {
    this.bindEvents();
  }

  bindEvents() {
    this.close.nativeElement.addEventListener('click', () => this.hideSidebar());
    this.helpOverlay.nativeElement.addEventListener('click', () => this.hideHelp());
    this.helpClose.nativeElement.addEventListener('click', () => this.hideHelp());
  }

  toggleSidebar() {
    this.sidebar.nativeElement.classList.toggle('active');
    this.overlay.nativeElement.classList.toggle('active');
  }

  hideSidebar() {
    this.sidebar.nativeElement.classList.remove('active');
    this.overlay.nativeElement.classList.remove('active');
  }

  showHelp() {
    this.help.nativeElement.classList.add('active');
    this.helpOverlay.nativeElement.classList.add('active');
  }

  hideHelp() {
    this.help.nativeElement.classList.remove('active');
    this.helpOverlay.nativeElement.classList.remove('active');
  }

  isActive(link: string): boolean {
    return this.currentUrl.startsWith(link);
  }

  isDropdownActive(baseUrl: string): boolean {
    return this.currentUrl.startsWith(baseUrl);
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
