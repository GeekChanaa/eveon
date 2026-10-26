import { Component, ElementRef, HostListener, OnInit, ViewChild } from '@angular/core';
import { FormControl } from '@angular/forms';
import { Router } from '@angular/router';
import { AccessService } from 'src/_services/access.service';
import { Observable, BehaviorSubject, debounceTime, distinctUntilChanged, switchMap, of } from 'rxjs';

interface SearchResult {
  type: 'recent' | 'suggestion' | 'route';
  title: string;
  description?: string;
  route?: string;
  icon?: string;
  thumbnail?: string;
}

@Component({
  selector: 'app-navbar-search',
  templateUrl: './navbar-search.component.html',
  styleUrls: ['./navbar-search.component.sass']
})
export class NavbarSearchComponent implements OnInit {

  @ViewChild('searchInput') searchInput!: ElementRef;
  
  searchControl = new FormControl('');
  isActive = false;
  isVisible = false;
  searchResults$: Observable<SearchResult[]>;
  recentSearches: SearchResult[] = [];
  
  /** Pages per shell, mirroring the sidebar: the navbar is shared by the operator, partner and customer dashboards. */
  private readonly operatorRoutes = [
    { path: "/dashboard", title: "Dashboard", icon: "home" },
    { path: "/dashboard/connector-realtime", title: "Connector Realtime", icon: "type2-charger" },
    { path: "/dashboard/alarm-management", title: "Alarm Management", icon: "warning" },
    { path: "/dashboard/statistics", title: "Statistics", icon: "bar-chart" },
    { path: "/dashboard/charging-stations", title: "Charging Stations", icon: "charging-station" },
    { path: "/dashboard/charging-points", title: "Charging Points", icon: "charging-cable" },
    { path: "/dashboard/charging-cards", title: "Charging Cards", icon: "ticket" },
    { path: "/dashboard/notices", title: "Notices", icon: "notification" },
    { path: "/dashboard/reports", title: "Reports", icon: "report" },
    { path: "/dashboard/user-info-download-requests", title: "Data Download Requests", icon: "download" },
    { path: "/dashboard/charging-sessions", title: "Charging Sessions", icon: "lightning" },
    { path: "/dashboard/transactions", title: "Transactions", icon: "payment" },
    { path: "/dashboard/recharge-orders", title: "Recharge Orders", icon: "basket" },
    { path: "/dashboard/invoices", title: "Invoices", icon: "invoice" },
    { path: "/dashboard/settlements", title: "Settlements", icon: "money-bill" },
    { path: "/dashboard/global-configurations", title: "Global Configurations", icon: "setting" },
    { path: "/dashboard/system-reports", title: "System Reports", icon: "report" },
    { path: "/dashboard/charger-events", title: "Charger Events", icon: "activity" },
    { path: "/dashboard/charge-point-configurations", title: "Charge Point Configurations", icon: "maintenance" },
    { path: "/dashboard/ocpp-configuration", title: "Default Configuration", icon: "setting" },
    { path: "/dashboard/ocpp-local-list", title: "Local List", icon: "list" },
    { path: "/dashboard/users", title: "Users", icon: "profile-circle" },
    { path: "/dashboard/partners", title: "Partners", icon: "store" },
    { path: "/dashboard/roles", title: "Roles & Permissions", icon: "account-setting" },
    { path: "/dashboard/audit-logs", title: "Audit Log", icon: "clock" },
    { path: "/dashboard/ocpi", title: "Roaming (OCPI)", icon: "share" },
    { path: "/dashboard/pki", title: "Charger PKI", icon: "lock" },
    { path: "/dashboard/documentation", title: "Documentation", icon: "help" },
    { path: "/dashboard/profile", title: "Profile", icon: "profile-circle" }
  ];

  private readonly partnerRoutes = [
    { path: "/partner-dashboard", title: "Dashboard", icon: "home" },
    { path: "/partner-dashboard/stations", title: "Charging Stations", icon: "charging-station" },
    { path: "/partner-dashboard/charge-points", title: "Charge Points & Prices", icon: "charging-cable" },
    { path: "/partner-dashboard/sessions", title: "Charging Sessions", icon: "lightning" },
    { path: "/partner-dashboard/statistics", title: "Statistics", icon: "bar-chart" },
    { path: "/partner-dashboard/statements", title: "Statements", icon: "invoice" },
    { path: "/partner-dashboard/profile", title: "Profile", icon: "profile-circle" },
    { path: "/partner-dashboard/security", title: "Security", icon: "lock" }
  ];

  private readonly customerRoutes = [
    { path: "/my-dashboard", title: "Dashboard", icon: "home" },
    { path: "/my-dashboard/sessions", title: "Charging Sessions", icon: "lightning" },
    { path: "/my-dashboard/cards", title: "My Cards", icon: "ticket" },
    { path: "/my-dashboard/wallet", title: "Wallet & Top-up", icon: "money-bill" },
    { path: "/my-dashboard/payment-methods", title: "Payment Methods", icon: "payment" },
    { path: "/my-dashboard/profile", title: "Profile", icon: "profile-circle" }
  ];

  private searchResultsSubject = new BehaviorSubject<SearchResult[]>([]);

  constructor(private router: Router, private access: AccessService) {
    this.searchResults$ = this.searchResultsSubject.asObservable();
  }

  /** The pages of the shell being browsed; operator pages are limited to the ones the user may open. */
  private get routes() {
    const url = this.router.url;
    if (url.startsWith('/my-dashboard')) return this.customerRoutes;
    if (url.startsWith('/partner-dashboard')) return this.partnerRoutes;
    return this.operatorRoutes.filter(route => this.access.canUrl(route.path));
  }

  /**
   * Recent searches kept in the browser, matched back to the current shell's pages: entries saved with an old
   * route, or from another shell, are dropped, and each page is listed once.
   */
  private loadRecentSearches(): void {
    let saved: SearchResult[] = [];
    try {
      saved = JSON.parse(localStorage.getItem('recentSearches') || '[]');
    } catch { }

    const routes = this.routes;
    const pages = (Array.isArray(saved) ? saved : [])
      .map(item => routes.find(route => route.path === item.route))
      .filter((route, index, all) => !!route && all.indexOf(route) === index);

    this.recentSearches = pages.slice(0, 3).map(route => ({
      type: 'recent' as const,
      title: route!.title,
      description: `Navigate to ${route!.title}`,
      route: route!.path,
      icon: route!.icon
    }));
  }

  ngOnInit(): void {
    this.searchControl.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap((term : any) => this.search(term))
    ).subscribe(results => {
      this.searchResultsSubject.next(results);
    });
  }

  search(term: string): Observable<SearchResult[]> {
    if (!term || term.length < 2) {
      return of([...this.recentSearches]);
    }
    
    const termLower = term.toLowerCase();
    
    // Find matching routes
    const routeResults = this.routes
      .filter(route => route.title.toLowerCase().includes(termLower))
      .map(route => ({
        type: 'route' as const,
        title: route.title,
        description: `Navigate to ${route.title}`,
        route: route.path,
        icon: route.icon
      }));
    
    // A page saved in the recent searches is also a route match: list each page once.
    const recentResults = this.recentSearches.filter(item => item.title.toLowerCase().includes(termLower));
    const recentRoutes = new Set(recentResults.map(item => item.route));

    return of([
      ...recentResults,
      ...routeResults.filter(result => !recentRoutes.has(result.route))
    ]);
  }

  toggleSearch(): void {
    this.isActive = !this.isActive;
    this.isVisible = this.isActive;
    
    if (this.isActive) {
      this.loadRecentSearches();
      setTimeout(() => {
        this.searchInput.nativeElement.focus();
      }, 100);
    } else {
      this.searchControl.setValue('');
    }
  }

  navigateTo(result: SearchResult): void {
    if (result.route) {
      this.saveToRecentSearches(result);
      this.router.navigate([result.route]);
      this.toggleSearch();
    }
  }
  
  removeFromRecent(result: SearchResult, event: Event): void {
    event.stopPropagation();
    this.recentSearches = this.recentSearches.filter(item => item.route !== result.route);
    localStorage.setItem('recentSearches', JSON.stringify(this.recentSearches));
  }
  
  private saveToRecentSearches(result: SearchResult): void {
    // Remove if exists already
    this.recentSearches = this.recentSearches.filter(item => item.route !== result.route);
    
    // Add to beginning
    this.recentSearches.unshift({
      type: 'recent',
      title: result.title,
      description: result.description,
      route: result.route,
      icon: result.icon,
      thumbnail: result.thumbnail
    });
    
    // Keep only latest 5
    if (this.recentSearches.length > 5) {
      this.recentSearches = this.recentSearches.slice(0, 5);
    }
    
    localStorage.setItem('recentSearches', JSON.stringify(this.recentSearches));
  }
  
  @HostListener('document:keydown.escape')
  closeSearch(): void {
    if (this.isActive) {
      this.toggleSearch();
    }
  }
  
  @HostListener('document:keydown.control.f', ['$event'])
  @HostListener('document:keydown.meta.f', ['$event'])
  onKeydownHandler(event: KeyboardEvent) {
    event.preventDefault();
    this.toggleSearch();
  }

}
