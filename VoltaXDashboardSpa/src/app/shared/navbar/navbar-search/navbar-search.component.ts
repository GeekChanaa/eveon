import { Component, ElementRef, HostListener, OnInit, ViewChild } from '@angular/core';
import { FormControl } from '@angular/forms';
import { Router } from '@angular/router';
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
  styleUrls: ['./navbar-search.component.css']
})
export class NavbarSearchComponent implements OnInit {

  @ViewChild('searchInput') searchInput!: ElementRef;
  
  searchControl = new FormControl('');
  isActive = false;
  isVisible = false;
  searchResults$: Observable<SearchResult[]>;
  recentSearches: SearchResult[] = [];
  
  private routes = [
    { path: "/dashboard/", title: "Dashboard", icon: "dashboard" },
    { path: "/dashboard/alarm-management", title: "Alarm Management", icon: "warning" },
    { path: "/dashboard/charging-cards", title: "Charging Cards", icon: "credit-card" },
    { path: "/dashboard/charging-points", title: "Charging Points", icon: "location" },
    { path: "/dashboard/charging-profile", title: "Charging Profile", icon: "settings" },
    { path: "/dashboard/charging-stationsf", title: "Charging Stations", icon: "ev-station" },
    { path: "/dashboard/charging-strategies", title: "Charging Strategies", icon: "strategy" },
    { path: "/dashboard/charging-sessions", title: "Charging Sessions", icon: "history" },
    { path: "/dashboard/comments", title: "Comments", icon: "comment" },
    { path: "/dashboard/reports", title: "Reports", icon: "assessment" },
    { path: "/dashboard/statistics", title: "Statistics", icon: "bar-chart" },
    { path: "/dashboard/users", title: "Users", icon: "people" },
    { path: "/dashboard/transactions", title: "Transactions", icon: "payment" },
    { path: "/dashboard/documentation", title: "Documentation", icon: "description" },
    { path: "/dashboard/partners", title: "Partners", icon: "handshake" },
    { path: "/dashboard/profile", title: "Profile", icon: "person" }
  ];
  
  private searchResultsSubject = new BehaviorSubject<SearchResult[]>([]);

  constructor(private router: Router) {
    // Load recent searches from localStorage
    const savedSearches = localStorage.getItem('recentSearches');
    if (savedSearches) {
      this.recentSearches = JSON.parse(savedSearches).slice(0, 3);
    }
    
    this.searchResults$ = this.searchResultsSubject.asObservable();
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
    
    return of([
      ...this.recentSearches.filter(item => 
        item.title.toLowerCase().includes(termLower) || 
        (item.description && item.description.toLowerCase().includes(termLower))
      ),
      ...routeResults
    ]);
  }

  toggleSearch(): void {
    this.isActive = !this.isActive;
    this.isVisible = this.isActive;
    
    if (this.isActive) {
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
    this.recentSearches = this.recentSearches.filter(item => item.title !== result.title);
    localStorage.setItem('recentSearches', JSON.stringify(this.recentSearches));
  }
  
  private saveToRecentSearches(result: SearchResult): void {
    // Remove if exists already
    this.recentSearches = this.recentSearches.filter(item => item.title !== result.title);
    
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
