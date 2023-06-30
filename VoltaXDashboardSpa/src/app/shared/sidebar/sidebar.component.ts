import { AfterViewInit, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { UserRole } from 'src/_models/_enums/user-role';
import { AuthService } from 'src/_services/auth.service';

@Component({
  selector: 'app-sidebar',
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.css']
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

  // User Role
  role : UserRole = UserRole.Customer;

  constructor(
    private _authService : AuthService
  ) { }

  ngOnInit() {
    this.role = this._authService.getRole();
  }

  ngAfterViewInit() {
    this.bindEvents();
  }

  bindEvents() {
    // Add event listeners
    // this.toggle.nativeElement.addEventListener('click', () => this.toggleSidebar());
    // this.helpOverlay.nativeElement.addEventListener('click', () => this.hideSidebar());
    this.close.nativeElement.addEventListener('click', () => this.hideSidebar());
    // this.helpOpen.nativeElement.addEventListener('click', () => this.showHelp());
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

}
